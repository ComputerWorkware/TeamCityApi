using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TeamCityApi.Clients;
using TeamCityApi.Domain;
using TeamCityApi.Helpers;
using TeamCityApi.Helpers.Graphs;
using TeamCityApi.Logging;

namespace TeamCityApi.UseCases
{
    public class CloneChildBuildConfigUseCase
    {
        private static readonly ILog Log = LogProvider.GetLogger(typeof(CloneChildBuildConfigUseCase));

        private readonly ITeamCityClient _client;

        private BuildConfigChain _buildConfigChain;
        private List<BuildSummary> _buildsInSourceSnapshotChain;
        private long _initialSourceBuildId;
        private string _newNameSuffix;
        private string _targetBuildChainId;
        private BuildConfig _sourceBuildConfig;
        private BuildConfig _targetRootBuildConfig;
        private readonly Dictionary<string, BuildConfig> _clones = new Dictionary<string, BuildConfig>();
    
        private bool _simulate;

        public CloneChildBuildConfigUseCase(ITeamCityClient client)
        {
            _client = client;
        }

        public async Task Execute(long sourceBuildId, string targetRootBuildConfigId, bool simulate)
        {
            Log.InfoFormat("Clone Child Build Config. sourceBuildId: {0}, targetRootBuildConfigId: {1}", sourceBuildId, targetRootBuildConfigId);

            await Init(sourceBuildId, targetRootBuildConfigId, simulate);

            var buildConfigsToClone = GetBuildConfigsToClone();

            var cloneBuildConfigCommands = await GetCloneBuildConfigsCommands(buildConfigsToClone);
            await Task.WhenAll(cloneBuildConfigCommands.Select(c =>
            {
                Log.InfoFormat("==== {0} ====", c);
                if (_simulate)
                    return Task.FromResult(0);
                else
                    return c.Execute();
            }));
            
            var swapDependencyCommands = GetSwapDependenciesCommands(buildConfigsToClone);
            await Task.WhenAll(swapDependencyCommands.Select(c =>
            {
                Log.InfoFormat("==== {0} ====", c);
                if (_simulate)
                    return Task.FromResult(0);
                else
                    return c.Execute();
            }));
        }

        private async Task Init(long sourceBuildId, string targetRootBuildConfigId, bool simulate)
        {
            _simulate = simulate;
            var sourceBuild = await _client.Builds.ById(sourceBuildId.ToString());
            _sourceBuildConfig = await _client.BuildConfigs.GetByConfigurationId(sourceBuild.BuildTypeId);
            _targetRootBuildConfig = await _client.BuildConfigs.GetByConfigurationId(targetRootBuildConfigId);

            if (_targetRootBuildConfig.Parameters[ParameterName.ClonedFromBuildId] == null)
                throw new Exception(
                    string.Format("Target root Build Config doesn't appear to be cloned. It is missing the \"{0}\" parameter.",
                        ParameterName.ClonedFromBuildId));

            _initialSourceBuildId = sourceBuildId;
            _targetBuildChainId = _targetRootBuildConfig.Parameters[ParameterName.BuildConfigChainId].Value;
            _newNameSuffix = _targetRootBuildConfig.Parameters[ParameterName.CloneNameSuffix].Value;
            _buildConfigChain = new BuildConfigChain(_client.BuildConfigs, _targetRootBuildConfig);
            _buildsInSourceSnapshotChain =
                await _client.Builds.ByBuildLocator(locator => locator.WithSnapshotDependencyFrom(long.Parse(sourceBuild.Id)));

            if (!_buildConfigChain.Contains(_sourceBuildConfig))
                throw new Exception(
                    string.Format(
                        "Cannot clone Build Config, because requested source Build (id:{0}, buildConfigId: {1}) is not found in the current Build Config chain for target Build Config ({2}). Make sure root Build Config depends on source Build's Build Config.",
                        sourceBuildId, _sourceBuildConfig.Id, targetRootBuildConfigId));

            if (_sourceBuildConfig.Parameters[ParameterName.ClonedFromBuildId]?.Value == _targetBuildChainId)
                throw new Exception(
                    string.Format(
                        "It appears that Build Config \"{0}\" is already a cloned for target Build Config \"{1}\", because \"{2}\" parameter is the same \"{3}\" . Create a new clone of root Build Config first",
                        _sourceBuildConfig.Id, _targetRootBuildConfig.Id, ParameterName.ClonedFromBuildId,
                        _sourceBuildConfig.Parameters[ParameterName.ClonedFromBuildId]));
        }

        private HashSet<BuildConfig> GetBuildConfigsToClone()
        {
            var buildConfigsToClone = _buildConfigChain.FindAllParents(_sourceBuildConfig);
            buildConfigsToClone.ExceptWith(new List<BuildConfig>() { _targetRootBuildConfig });
            buildConfigsToClone.Add(_sourceBuildConfig);
            return buildConfigsToClone;
        }

        private async Task<List<CloneBuildConfigCommand>> GetCloneBuildConfigsCommands(HashSet<BuildConfig> buildConfigsToClone)
        {
            var cloneBuildConfigCommands = new List<CloneBuildConfigCommand>() {};

            foreach (var parentToClone in buildConfigsToClone)
            {
                var buildToCloneFrom = await GetBuildFromChain(parentToClone.Id);
                cloneBuildConfigCommands.Add(new CloneBuildConfigCommand(this, buildToCloneFrom));
            }

            return cloneBuildConfigCommands;
        }

        private List<SwapDependencyCommand> GetSwapDependenciesCommands(HashSet<BuildConfig> buildConfigsToClone)
        {
            var swapDependencyCommands = new List<SwapDependencyCommand>();
            foreach (var buildConfigToClone in buildConfigsToClone)
            {
                var parentBuildConfigs = _buildConfigChain.GetParents(buildConfigToClone);

                foreach (var parentBuildConfig in parentBuildConfigs)
                {
                    var targetBuildConfig = buildConfigsToClone.Contains(parentBuildConfig) ? GetCloneOf(parentBuildConfig) : parentBuildConfig;

                    swapDependencyCommands.Add(new SwapDependencyCommand(this, targetBuildConfig, GetCloneOf(buildConfigToClone), buildConfigToClone.Id ));
                }
            }
            return swapDependencyCommands;
        }

        private BuildConfig GetCloneOf(BuildConfig buildConfigToClone)
        {
            if (_simulate)
            {
                var simulatedClone = new BuildConfig();
                simulatedClone.Name = buildConfigToClone.Name + " Clone";
                simulatedClone.Id = buildConfigToClone.Id + "_Clone";
                return simulatedClone;
            }

            return _clones[buildConfigToClone.Id];
        }

        private async Task<Build> GetBuildFromChain(string buildConfigId)
        {
            var parentBuildSummary = _buildsInSourceSnapshotChain.FirstOrDefault(b => b.BuildTypeId == buildConfigId);
            if (parentBuildSummary == null)
            {
                var buildsInChainDebugDisplay = string.Join(Environment.NewLine,
                    _buildsInSourceSnapshotChain.Select(b => b.Id + " (" + b.BuildTypeId + ")"));

                throw new Exception(string.Format("Cannot find a build by Build Config Id \"{0}\" in build chain with source Build Id \"{1}\". " + Environment.NewLine +
                                                  "Found Build Ids (Build Config Ids) are: " + Environment.NewLine + buildsInChainDebugDisplay,
                                                   buildConfigId, _initialSourceBuildId));
            }

            return await _client.Builds.ById(parentBuildSummary.Id);
        }

        public async Task<BuildConfig> CloneBuildConfig(Build sourceBuild)
        {
            //Log.DebugFormat("CopyBuildConfigurationFromBuild(sourceBuild: {0}, previouslyClonedBuildConfig: {1}, previouslyClonedFromBuildConfigId: {1})", sourceBuild, previouslyClonedBuildConfig, previouslyClonedFromBuildConfigId);

            var newBuildConfig = await _client.BuildConfigs.CopyBuildConfiguration(
                sourceBuild.BuildConfig.ProjectId,
                BuildConfig.NewName(sourceBuild.BuildConfig.Name, _newNameSuffix),
                sourceBuild.BuildConfig.Id
            );

            _clones.Add(sourceBuild.BuildConfig.Id, newBuildConfig);

            await _client.BuildConfigs.DeleteAllSnapshotDependencies(newBuildConfig);
            await _client.BuildConfigs.FreezeAllArtifactDependencies(newBuildConfig, sourceBuild);
            await _client.BuildConfigs.FreezeParameters(newBuildConfig, newBuildConfig.Parameters.Property, sourceBuild.Properties.Property);
            await _client.BuildConfigs.SetParameterValue(newBuildConfig, ParameterName.ClonedFromBuildId, sourceBuild.Id);
            await _client.BuildConfigs.SetParameterValue(newBuildConfig, ParameterName.BuildConfigChainId, _targetBuildChainId);

            return newBuildConfig;
        }

        public async Task SwapDependencies(BuildConfig targetBuildConfig, BuildConfig buildConfigToSwapTo, string buildConfigIdToSwapFrom)
        {
            //Log.DebugFormat("SwapDependenciesToPreviouslyClonedBuildConfig(targetBuildConfig: {0}, previouslyClonedBuildConfig: {1}, previouslyClonedBuildConfigFromBuild: {2})", targetBuildConfig, previouslyClonedBuildConfig, previouslyClonedFromBuildConfigId);

            var artifactDependencyToSwap = targetBuildConfig.ArtifactDependencies.FirstOrDefault(a => a.SourceBuildConfig.Id == buildConfigIdToSwapFrom);
            if (artifactDependencyToSwap == null)
                throw new Exception(String.Format("Cannot find targetBuildConfig.ArtifactDependencies by SourceBuildConfig.Id == {0}. Available SourceBuildConfig.Ids are: {1}", buildConfigIdToSwapFrom, String.Join(", ", targetBuildConfig.ArtifactDependencies.Select(ad => ad.SourceBuildConfig.Id))));

            artifactDependencyToSwap.Properties.Property["revisionName"].Value = "sameChainOrLastFinished";
            artifactDependencyToSwap.Properties.Property["revisionValue"].Value = "latest.sameChainOrLastFinished";
            artifactDependencyToSwap.SourceBuildConfig.Id = buildConfigToSwapTo.Id;
            artifactDependencyToSwap.SourceBuildConfig.ProjectId = buildConfigToSwapTo.ProjectId;
            artifactDependencyToSwap.SourceBuildConfig.ProjectName = buildConfigToSwapTo.ProjectName;

            await _client.BuildConfigs.UpdateArtifactDependency(targetBuildConfig.Id, artifactDependencyToSwap);

            await _client.BuildConfigs.CreateSnapshotDependency(new CreateSnapshotDependency(targetBuildConfig.Id, buildConfigToSwapTo.Id));
        }

        private class CloneBuildConfigCommand : ICommand
        {
            private readonly CloneChildBuildConfigUseCase _receiver;
            private readonly Build _sourceBuild;

            public CloneBuildConfigCommand(CloneChildBuildConfigUseCase receiver, Build sourceBuild)
            {
                _receiver = receiver;
                _sourceBuild = sourceBuild;
            }

            public async Task Execute()
            {
                await _receiver.CloneBuildConfig(_sourceBuild);
            }

            public override string ToString()
            {
                return string.Format("Clone {1} from build id:{0}", _sourceBuild.Id, _sourceBuild.BuildConfig.Id);
            }
        }

        private class SwapDependencyCommand : ICommand
        {
            private readonly CloneChildBuildConfigUseCase _receiver;
            private readonly BuildConfig _targetBuildConfig;
            private readonly BuildConfig _buildConfigToSwapTo;
            private readonly string _buildConfigIdToSwapFrom;

            public SwapDependencyCommand(CloneChildBuildConfigUseCase receiver, BuildConfig targetBuildConfig, BuildConfig buildConfigToSwapTo, string buildConfigIdToSwapFrom)
            {
                _receiver = receiver;
                _targetBuildConfig = targetBuildConfig;
                _buildConfigToSwapTo = buildConfigToSwapTo;
                _buildConfigIdToSwapFrom = buildConfigIdToSwapFrom;
            }

            public async Task Execute()
            {
                await _receiver.SwapDependencies(_targetBuildConfig, _buildConfigToSwapTo, _buildConfigIdToSwapFrom);
            }

            public override string ToString()
            {
                return string.Format("Swap dependencies on {0}: {1} => {2}", _targetBuildConfig.Id, _buildConfigIdToSwapFrom, _buildConfigToSwapTo.Id);
            }

        }

        private interface ICommand
        {
            Task Execute();
        }
    }
}