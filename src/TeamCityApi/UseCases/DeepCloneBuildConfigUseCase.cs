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
    public class DeepCloneBuildConfigUseCase
    {
        private static readonly ILog Log = LogProvider.GetLogger(typeof(CloneChildBuildConfigUseCase));

        private readonly ITeamCityClient _client;
        private readonly IVcsRootHelper _vcsRootHelper;
        private readonly IBuildConfigXmlClient _buildConfigXmlClient;

        private BuildChain _buildChain;
        private string _newNameSuffix;
        private string _newBranchName;
        private string _buildChainId;
        private BuildConfig _sourceBuildConfig;
        private readonly Dictionary<string, IBuildConfigXml> _originalBuildConfigIdToCloneMap = new Dictionary<string, IBuildConfigXml>();

        private bool _simulate;

        public DeepCloneBuildConfigUseCase(ITeamCityClient client, IVcsRootHelper vcsRootHelper, IBuildConfigXmlClient buildConfigXmlClient)
        {
            _client = client;
            _vcsRootHelper = vcsRootHelper;
            _buildConfigXmlClient = buildConfigXmlClient;
        }

        public async Task Execute(long sourceBuildId, string newNameSuffix, bool simulate)
        {
            Log.InfoFormat("Deep Clone Build Config. sourceBuildId: {0}, newNameSuffix: {1}", sourceBuildId, newNameSuffix);

            var sourceBuild = await _client.Builds.ById(sourceBuildId);

            _buildConfigXmlClient.Simulate = simulate;

            _simulate = simulate;
            _sourceBuildConfig = await _client.BuildConfigs.GetByConfigurationId(sourceBuild.BuildTypeId);
            _newNameSuffix = newNameSuffix;
            _newBranchName = VcsRootHelper.ToValidGitBranchName(_newNameSuffix);
            _buildChainId = Guid.NewGuid().ToString();
            _buildChain = new BuildChain(_client.Builds, sourceBuild);

            var buildConfigsToClone = await GetBuildsToClone();

            foreach (var b in buildConfigsToClone)
            {
                Log.Info($"==== Branch {b.HistoricBuild.BuildTypeId} from Build #{b.HistoricBuild.Number} (id: {b.HistoricBuild.Id}) ====");
                if (!_simulate)
                {
                    await _vcsRootHelper.BranchUsingGitLabApi(b.HistoricBuild.Id, _newBranchName);
                }
            }

            var cloneBuildConfigCommands = GetCloneBuildConfigsCommands(buildConfigsToClone.ToList());
            foreach (var c in cloneBuildConfigCommands)
            {
                Log.Info($"==== {c} ====");
                if (!_simulate)
                    c.Execute();
            }

            var swapDependencyCommands = GetSwapDependenciesCommands(buildConfigsToClone);
            foreach (var c in swapDependencyCommands)
            {
                Log.Info($"==== {c} ====");
                if (!_simulate)
                    c.Execute();
            }

            if (!_simulate)
                _buildConfigXmlClient.Push();
        }

        private async Task<HashSet<DependencyNode>> GetBuildsToClone()
        {
            var buildsToClone = new HashSet<DependencyNode>();
            foreach (var buildNode in _buildChain.Nodes)
            {
                if (buildsToClone.Any(bc => bc.CurrentBuildConfig.Id == buildNode.Value.BuildTypeId))
                {
                    throw new Exception($"Build configuration {buildNode.Value.BuildTypeId} was already added to chain. " +
                                        $"Build chain likely contains duplicate builds for the single configuration." +
                                        $"To prevent it make sure that there is a snapshot dependency for each artifact dependency.");
                }

                var buildConfig = await _client.BuildConfigs.GetByConfigurationId(buildNode.Value.BuildTypeId);
                buildsToClone.Add(new DependencyNode(buildConfig, buildNode.Value));
            }

            return buildsToClone;
        }

        private IEnumerable<CloneBuildConfigCommand> GetCloneBuildConfigsCommands(IEnumerable<DependencyNode> buildConfigsToClone)
        {
            return buildConfigsToClone.Select(bc => new CloneBuildConfigCommand(this, bc.HistoricBuild));
        }

        private IEnumerable<SwapDependencyCommand> GetSwapDependenciesCommands(IEnumerable<DependencyNode> clonedBuildConfigs)
        {
            var swapDependencyCommands = new HashSet<SwapDependencyCommand>();
            foreach (var clonedBuildConfig in clonedBuildConfigs)
            {
                var parentBuilds = _buildChain.GetParents(clonedBuildConfig.HistoricBuild.BuildTypeId);

                foreach (var parentBuild in parentBuilds)
                {
                    var parentBuildConfig = _client.BuildConfigs.GetByConfigurationId(parentBuild.BuildTypeId).Result;
                    Lazy<IBuildConfigXml> swapOnXml;
	                string swapOnId;
					var parentBuildConfigWasJustCloned = clonedBuildConfigs.Contains(new DependencyNode(parentBuildConfig, parentBuild));
                    if (parentBuildConfigWasJustCloned)
                    {
	                    swapOnId = parentBuildConfig.Id;
						swapOnXml = new Lazy<IBuildConfigXml>(() => GetCloneOf(swapOnId));
                    }
                    else
                    {
						//defer read from file as we want to read the latest version (including previously swapped dependencies)
						//if file is read here for each command then swapping each dependency will discard previous changes
						swapOnId = parentBuildConfig.Id;
						swapOnXml = new Lazy<IBuildConfigXml>(() => _buildConfigXmlClient.Read(parentBuildConfig.ProjectId, swapOnId));
                    }

                    var swapFrom = clonedBuildConfig.HistoricBuild.BuildTypeId;
                    var swapTo = GetCloneOf(clonedBuildConfig.HistoricBuild.BuildTypeId).BuildConfigId;

                    swapDependencyCommands.Add(new SwapDependencyCommand(this, swapOnXml, swapOnId, swapTo, swapFrom));
                }
            }
            return swapDependencyCommands;
        }

        private IBuildConfigXml GetCloneOf(string buildConfigToCloneId)
        {
            if (_simulate)
            {
                var simulatedClone = new BuildConfigXml(null, "", buildConfigToCloneId + "_Clone");
                return simulatedClone;
            }

            if (!_originalBuildConfigIdToCloneMap.ContainsKey(buildConfigToCloneId))
            {
                throw new Exception($"Could not find key \"{buildConfigToCloneId}\" in {nameof(_originalBuildConfigIdToCloneMap)}");
            }

            return _originalBuildConfigIdToCloneMap[buildConfigToCloneId];
        }

        public IBuildConfigXml CloneBuildConfig(Build sourceBuild)
        {
            //Log.DebugFormat("CopyBuildConfigurationFromBuild(sourceBuild: {0}, previouslyClonedBuildConfig: {1}, previouslyClonedFromBuildConfigId: {1})", sourceBuild, previouslyClonedBuildConfig, previouslyClonedFromBuildConfigId);
            var originalCurrentBuildConfig = _client.BuildConfigs.GetByConfigurationId(sourceBuild.BuildConfig.Id).Result;

            var newName = BuildConfig.NewName(sourceBuild.BuildConfig.Name, _newNameSuffix);
            var newBuildConfigId = _client.BuildConfigs.GenerateUniqueBuildConfigId(sourceBuild.BuildConfig.ProjectId, newName).Result;

            var newBuildConfigXml = _buildConfigXmlClient.ReadAsOf(sourceBuild.BuildConfig.ProjectId, sourceBuild.BuildConfig.Id, sourceBuild.StartDate);

            var clonedBuildConfigXml = newBuildConfigXml.CopyBuildConfiguration(newBuildConfigId, newName);

            _originalBuildConfigIdToCloneMap.Add(sourceBuild.BuildConfig.Id, clonedBuildConfigXml);

            clonedBuildConfigXml.DeleteAllSnapshotDependencies();
            clonedBuildConfigXml.FreezeParameters(sourceBuild.Properties.Property);
            clonedBuildConfigXml.SetParameterValue(ParameterName.ClonedFromBuildId, sourceBuild.Id.ToString());
            clonedBuildConfigXml.SetParameterValue(ParameterName.BuildConfigChainId, _buildChainId);
            clonedBuildConfigXml.SetParameterValue(ParameterName.BranchName, _newBranchName);
            clonedBuildConfigXml.SwitchTemplateAndRepoToCurrentState(originalCurrentBuildConfig);

            return clonedBuildConfigXml;
        }

        public void SwapDependenciesToClone(Lazy<IBuildConfigXml> swapOn, string swapTo, string swapFrom)
        {
            //Log.DebugFormat("SwapDependenciesToPreviouslyClonedBuildConfig(swapOn: {0}, previouslyClonedBuildConfig: {1}, previouslyClonedBuildConfigFromBuild: {2})", swapOn, previouslyClonedBuildConfig, previouslyClonedFromBuildConfigId);

            var swapOnBuildConfigXml = swapOn.Value;

            swapOnBuildConfigXml.UpdateArtifactDependency(swapFrom, swapTo, "sameChainOrLastFinished", "latest.sameChainOrLastFinished");

            swapOnBuildConfigXml.CreateSnapshotDependency(swapTo);
        }

        private class CloneBuildConfigCommand : ICommand
        {
            private readonly DeepCloneBuildConfigUseCase _receiver;
            private readonly Build _sourceBuild;

            public CloneBuildConfigCommand(DeepCloneBuildConfigUseCase receiver, Build sourceBuild)
            {
                _receiver = receiver;
                _sourceBuild = sourceBuild;
            }

            public void Execute()
            {
                _receiver.CloneBuildConfig(_sourceBuild);
            }

            public override string ToString()
            {
                return $"Clone {_sourceBuild.BuildConfig.Id} from Build #{_sourceBuild.Number}";
            }
        }

        private class SwapDependencyCommand : ICommand
        {
            private readonly DeepCloneBuildConfigUseCase _receiver;
            private readonly Lazy<IBuildConfigXml> _swapOnXml;
            private readonly string _swapOnId;
            private readonly string _swapTo;
            private readonly string _swapFrom;

            public SwapDependencyCommand(DeepCloneBuildConfigUseCase receiver, Lazy<IBuildConfigXml> swapOnXml, string swapOnId, string swapTo, string swapFrom)
            {
                _receiver = receiver;
				_swapOnXml = swapOnXml;
				_swapOnId = swapOnId;
                _swapTo = swapTo;
                _swapFrom = swapFrom;
            }

            public void Execute()
            {
                _receiver.SwapDependenciesToClone(_swapOnXml, _swapTo, _swapFrom);
            }

            public override string ToString()
            {
                return $"Swap dependencies on {_swapOnId}: {_swapFrom} => {_swapTo}";
            }

            protected bool Equals(SwapDependencyCommand other)
            {
                return Equals(_swapOnId, other._swapOnId) && string.Equals(_swapTo, other._swapTo) && string.Equals(_swapFrom, other._swapFrom);
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != this.GetType()) return false;
                return Equals((SwapDependencyCommand) obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    var hashCode = _swapOnId?.GetHashCode() ?? 0;
                    hashCode = (hashCode*397) ^ (_swapTo?.GetHashCode() ?? 0);
                    hashCode = (hashCode*397) ^ (_swapFrom?.GetHashCode() ?? 0);
                    return hashCode;
                }
            }
        }

        private interface ICommand
        {
            void Execute();
        }
    }
}