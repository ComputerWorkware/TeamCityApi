using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TeamCityApi.Clients;
using TeamCityApi.Domain;
using TeamCityApi.Helpers;
using TeamCityApi.Logging;

namespace TeamCityApi.UseCases
{
    public class CloneChildBuildConfigUseCase
    {
        private static readonly ILog Log = LogProvider.GetLogger(typeof(CloneChildBuildConfigUseCase));

        private readonly ITeamCityClient _client;

        private BuildConfigChain _buildConfigChain;
        private List<BuildSummary> _otherBuildsInSourceSnapshotChain;
        private long _initialSourceBuildId;
        private string _newNameSuffix;
        private string _targetBuildChainId;
        private BuildConfig _targetRootBuildConfig;

        public CloneChildBuildConfigUseCase(ITeamCityClient client)
        {
            _client = client;
        }

        public async Task Execute(long sourceBuildId, string targetRootBuildConfigId)
        {
            Log.InfoFormat("Clone Child Build Config. sourceBuildId: {0}, targetRootBuildConfigId: {1}", sourceBuildId, targetRootBuildConfigId);

            var sourceBuild = await _client.Builds.ById(sourceBuildId.ToString());
            var sourceBuildConfig = await _client.BuildConfigs.GetByConfigurationId(sourceBuild.BuildTypeId);
            _targetRootBuildConfig = await _client.BuildConfigs.GetByConfigurationId(targetRootBuildConfigId);

            if (_targetRootBuildConfig.Parameters[ParameterName.ClonedFromBuildId] == null)
                throw new Exception(string.Format("Target root Build Config doesn't appear to be cloned. It is missing the \"{0}\" parameter.", ParameterName.ClonedFromBuildId));

            _initialSourceBuildId = sourceBuildId;
            _targetBuildChainId = _targetRootBuildConfig.Parameters[ParameterName.BuildConfigChainId].Value;
            _newNameSuffix = _targetRootBuildConfig.Parameters[ParameterName.CloneNameSuffix].Value;
            _buildConfigChain = new BuildConfigChain(_client.BuildConfigs, _targetRootBuildConfig);
            _otherBuildsInSourceSnapshotChain = await _client.Builds.ByBuildLocator(locator => locator.WithSnapshotDependencyFrom(long.Parse(sourceBuild.Id)));
            
            if (!_buildConfigChain.Contains(sourceBuildConfig))
                throw new Exception(string.Format("Cannot clone Build Config, because requested source Build (id:{0}, buildConfigId: {1}) is not found in the current Build Config chain for target Build Config ({2}). Make sure root Build Config depends on source Build's Build Config.", sourceBuildId, sourceBuildConfig.Id, targetRootBuildConfigId));
            
            if (sourceBuildConfig.Parameters[ParameterName.ClonedFromBuildId]?.Value == _targetBuildChainId)
                throw new Exception(string.Format("It appears that Build Config \"{0}\" is already a cloned for target Build Config \"{1}\", because \"{2}\" parameter is the same \"{3}\" . Create a new clone of root Build Config first", sourceBuildConfig.Id, _targetRootBuildConfig.Id, ParameterName.ClonedFromBuildId, sourceBuildConfig.Parameters[ParameterName.ClonedFromBuildId]));

            await CloneRecursively(sourceBuild, sourceBuildConfig);
        }

        private async Task CloneRecursively(Build sourceBuild, BuildConfig sourceBuildConfig, BuildConfig prevClonedBuildConfig = null, string previouslyClonedFromBuildConfigId = "")
        {
            Log.Info("----------------------------------------------");
            Log.InfoFormat("Clone {0}", sourceBuildConfig.Id);

            var clonedBuildConfig = await CopyBuildConfigurationFromBuild(sourceBuild, prevClonedBuildConfig, previouslyClonedFromBuildConfigId);

            var parentsToClone = GetParentsToClone(sourceBuildConfig).ToList();
            var parentsNotToClone = _buildConfigChain.GetParents(sourceBuildConfig).Except(parentsToClone);

            foreach (var parentBuildConfig in parentsToClone)
            {
                var parentBuild = await LookupParentBuild(parentBuildConfig.Id);

                await CloneRecursively(parentBuild, parentBuildConfig, clonedBuildConfig, sourceBuild.BuildTypeId);

                _buildConfigChain = new BuildConfigChain(_client.BuildConfigs, _targetRootBuildConfig);
            }

            foreach (var parentBuildConfig in parentsNotToClone)
            {
                await SwapDependenciesToPreviouslyClonedBuildConfig(parentBuildConfig, clonedBuildConfig, sourceBuild.BuildTypeId);

                _buildConfigChain = new BuildConfigChain(_client.BuildConfigs, _targetRootBuildConfig);
            }
        }

        private async Task<BuildConfig> CopyBuildConfigurationFromBuild(Build sourceBuild, BuildConfig previouslyClonedBuildConfig, string previouslyClonedFromBuildConfigId)
        {
            //Log.DebugFormat("CopyBuildConfigurationFromBuild(sourceBuild: {0}, previouslyClonedBuildConfig: {1}, previouslyClonedFromBuildConfigId: {1})", sourceBuild, previouslyClonedBuildConfig, previouslyClonedFromBuildConfigId);

            var newBuildConfig = await _client.BuildConfigs.CopyBuildConfiguration(
                sourceBuild.BuildConfig.ProjectId,
                BuildConfig.NewName(sourceBuild.BuildConfig.Name, _newNameSuffix),
                sourceBuild.BuildConfig.Id
            );

            await _client.BuildConfigs.DeleteAllSnapshotDependencies(newBuildConfig);
            await _client.BuildConfigs.FreezeAllArtifactDependencies(newBuildConfig, sourceBuild);
            await _client.BuildConfigs.FreezeParameters(newBuildConfig, newBuildConfig.Parameters.Property, sourceBuild.Properties.Property);
            await _client.BuildConfigs.SetParameterValue(newBuildConfig, ParameterName.ClonedFromBuildId, sourceBuild.Id);
            await _client.BuildConfigs.SetParameterValue(newBuildConfig, ParameterName.BuildConfigChainId, _targetBuildChainId);

            if (previouslyClonedBuildConfig != null)
            {
                await SwapDependenciesToPreviouslyClonedBuildConfig(newBuildConfig, previouslyClonedBuildConfig, previouslyClonedFromBuildConfigId);
            }

            return newBuildConfig;
        }

        private async Task SwapDependenciesToPreviouslyClonedBuildConfig(BuildConfig targetBuildConfig, BuildConfig previouslyClonedBuildConfig, string previouslyClonedFromBuildConfigId)
        {
            //Log.DebugFormat("SwapDependenciesToPreviouslyClonedBuildConfig(targetBuildConfig: {0}, previouslyClonedBuildConfig: {1}, previouslyClonedBuildConfigFromBuild: {2})", targetBuildConfig, previouslyClonedBuildConfig, previouslyClonedFromBuildConfigId);

            var artifactDependencyToSwap = targetBuildConfig.ArtifactDependencies.FirstOrDefault(a => a.SourceBuildConfig.Id == previouslyClonedFromBuildConfigId);
            if (artifactDependencyToSwap == null)
                throw new Exception(String.Format("Cannot find targetBuildConfig.ArtifactDependencies by SourceBuildConfig.Id == {0}. Available SourceBuildConfig.Ids are: {1}", previouslyClonedFromBuildConfigId, String.Join(", ", targetBuildConfig.ArtifactDependencies.Select(ad => ad.SourceBuildConfig.Id))));

            artifactDependencyToSwap.Properties.Property["revisionName"].Value = "sameChainOrLastFinished";
            artifactDependencyToSwap.Properties.Property["revisionValue"].Value = "latest.sameChainOrLastFinished";
            artifactDependencyToSwap.SourceBuildConfig.Id = previouslyClonedBuildConfig.Id;
            artifactDependencyToSwap.SourceBuildConfig.ProjectId = previouslyClonedBuildConfig.ProjectId;
            artifactDependencyToSwap.SourceBuildConfig.ProjectName = previouslyClonedBuildConfig.ProjectName;

            await _client.BuildConfigs.UpdateArtifactDependency(targetBuildConfig.Id, artifactDependencyToSwap);

            await _client.BuildConfigs.CreateSnapshotDependency(new CreateSnapshotDependency(targetBuildConfig.Id, previouslyClonedBuildConfig.Id));
        }

        private IEnumerable<BuildConfig> GetParentsToClone(BuildConfig sourceBuildConfig)
        {
            var parentBuildConfigs = _buildConfigChain.GetParents(sourceBuildConfig);

            //filter out already cloned parents (same ConfigBuildChainId means parent was already cloned for this root)
            var parentToClone = parentBuildConfigs.Where(
                pbc => pbc.Parameters[ParameterName.BuildConfigChainId] == null || pbc.Parameters[ParameterName.BuildConfigChainId].Value != _targetBuildChainId);

            return parentToClone;
        }

        private async Task<Build> LookupParentBuild(string parentBuildConfigId)
        {
            var parentBuildSummary = _otherBuildsInSourceSnapshotChain.FirstOrDefault(b => b.BuildTypeId == parentBuildConfigId);
            if (parentBuildSummary == null)
                throw new Exception(string.Format("Cannot find a build for Build Config \"{0}\" in build chain with source Build \"{1}\"", parentBuildConfigId, _initialSourceBuildId));

            return await _client.Builds.ById(parentBuildSummary.Id);
        }
    }
}