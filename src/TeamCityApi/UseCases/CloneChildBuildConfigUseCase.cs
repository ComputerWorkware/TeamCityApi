using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TeamCityApi.Clients;
using TeamCityApi.Domain;
using TeamCityApi.Helpers;
using TeamCityApi.Helpers.Git;
using TeamCityApi.Helpers.Graphs;
using TeamCityApi.Logging;

namespace TeamCityApi.UseCases
{
    public class CloneChildBuildConfigUseCase
    {
        private static readonly ILog Log = LogProvider.GetLogger(typeof(CloneChildBuildConfigUseCase));

        private readonly ITeamCityClient _client;
        private readonly IVcsRootHelper _vcsRootHelper;
        private readonly IBuildConfigXmlClient _buildConfigXmlClient;

        private DependencyChain _dependencyChain;
        private string _newNameSuffix;
        private string _newBranchName;
        private string _targetBuildChainId;
        private BuildConfig _sourceBuildConfig;
        private BuildConfig _targetRootBuildConfig;
        private readonly Dictionary<string, IBuildConfigXml> _originalBuildConfigIdToCloneMap = new Dictionary<string, IBuildConfigXml>();

        private bool _simulate;

        public CloneChildBuildConfigUseCase(ITeamCityClient client, IVcsRootHelper vcsRootHelper, IBuildConfigXmlClient buildConfigXmlClient)
        {
            _client = client;
            _vcsRootHelper = vcsRootHelper;
            _buildConfigXmlClient = buildConfigXmlClient;
            
        }

        public async Task Execute(string sourceBuildConfigId, string targetRootBuildConfigId, bool simulate)
        {
            Log.Info($"Clone Child Build Config. sourceBuildConfigId: {sourceBuildConfigId}, targetRootBuildConfigId: {targetRootBuildConfigId}");

            _buildConfigXmlClient.Simulate = simulate;

            await Init(sourceBuildConfigId, targetRootBuildConfigId, simulate);

            var buildConfigsToClone = GetBuildsToClone();

            foreach (var b in buildConfigsToClone)
            {
                Log.Info($"==== Branch {b.HistoricBuild.BuildTypeId} from Build #{b.HistoricBuild.Number} (id: {b.HistoricBuild.Id}) ====");
                if (!_simulate)
                {
                    await _vcsRootHelper.CloneAndBranchAndPushAndDeleteLocalFolder(b.HistoricBuild.Id, _newBranchName);
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

        private async Task Init(string sourceBuildConfigId, string targetRootBuildConfigId, bool simulate)
        {
            _simulate = simulate;
            _sourceBuildConfig = await _client.BuildConfigs.GetByConfigurationId(sourceBuildConfigId);
            _targetRootBuildConfig = await _client.BuildConfigs.GetByConfigurationId(targetRootBuildConfigId);

            if (_targetRootBuildConfig.Parameters[ParameterName.ClonedFromBuildId] == null)
                throw new Exception(
                    $"Target root Build Config doesn't appear to be cloned. It is missing the \"{ParameterName.ClonedFromBuildId}\" parameter.");

            _targetBuildChainId = _targetRootBuildConfig.Parameters[ParameterName.BuildConfigChainId].Value;
            _newNameSuffix = _targetRootBuildConfig.Parameters[ParameterName.CloneNameSuffix].Value;
            _newBranchName = VcsRootHelper.ToValidGitBranchName(_newNameSuffix);
            _dependencyChain = new DependencyChain(_client, _targetRootBuildConfig);

            if (!_dependencyChain.Contains(_sourceBuildConfig.Id))
            {
                throw new Exception(
                    $"Cannot clone Build Config, because requested source Build Config ({_sourceBuildConfig.Id}) " +
                    $"is not found in the current Build Config chain for target Build Config ({targetRootBuildConfigId}). " +
                    $"Make sure target Build Config depends on source Build Config." + Environment.NewLine +
                    $"Currently discovered Build Config chain is: " + Environment.NewLine + "{_dependencyChain}");
            }

            if (_sourceBuildConfig.Parameters[ParameterName.BuildConfigChainId].Value == _targetBuildChainId)
                throw new Exception(
                    $"It appears that Build Config \"{_sourceBuildConfig.Id}\" is already a cloned for target " +
                    $"Build Config \"{_targetRootBuildConfig.Id}\", because \"{ParameterName.BuildConfigChainId}\" " +
                    $"parameter is the same \"{_sourceBuildConfig.Parameters[ParameterName.BuildConfigChainId]}\" . " +
                    $"Create a new clone of root Build Config first");
        }

        private HashSet<DependencyNode> GetBuildsToClone()
        {
            var sourceDependency = _dependencyChain.FindByBuildConfigId(_sourceBuildConfig.Id);
            var buildsToClone = _dependencyChain.FindAllParents(_sourceBuildConfig.Id);
            buildsToClone.Remove(new DependencyNode(_targetRootBuildConfig));
            buildsToClone.Add(sourceDependency);
            buildsToClone.RemoveWhere(d => d.IsCloned);

            return buildsToClone;
        }

        private IEnumerable<CloneBuildConfigCommand> GetCloneBuildConfigsCommands(IEnumerable<DependencyNode> buildConfigsToClone)
        {
            return buildConfigsToClone.Select(bc => new CloneBuildConfigCommand(this, bc.HistoricBuild));
        }

        private IEnumerable<SwapDependencyCommand> GetSwapDependenciesCommands(IEnumerable<DependencyNode> clonedBuildConfigs)
        {
            var swapDependencyCommands = new List<SwapDependencyCommand>();
            foreach (var buildConfigToClone in clonedBuildConfigs)
            {
                var parentBuildConfigs = _dependencyChain.GetParents(buildConfigToClone.HistoricBuild.BuildTypeId);

                foreach (var parentBuildConfig in parentBuildConfigs)
                {
                    Lazy<IBuildConfigXml> swapOn;
                    var parentBuildConfigWasJustCloned = clonedBuildConfigs.Contains(parentBuildConfig);
                    if (parentBuildConfigWasJustCloned)
                    {
                        swapOn = new Lazy<IBuildConfigXml>(() => GetCloneOf(parentBuildConfig.HistoricBuild.BuildTypeId));
                    }
                    else
                    {
                        //defer read from file as we want to read the latest version (including previously swapped dependencies)
                        //if file is read here for each command then swapping each dependency will discard previous changes
                        swapOn = new Lazy<IBuildConfigXml>(() => _buildConfigXmlClient.Read(parentBuildConfig.CurrentBuildConfig.ProjectId, parentBuildConfig.CurrentBuildConfig.Id));
                    }

                    var swapFrom = buildConfigToClone.HistoricBuild.BuildTypeId;
                    var swapTo = GetCloneOf(buildConfigToClone.HistoricBuild.BuildTypeId).BuildConfigId;

                    swapDependencyCommands.Add(new SwapDependencyCommand(this, swapOn, swapTo, swapFrom));
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

            var newName = BuildConfig.NewName(sourceBuild.BuildConfig.Name, _newNameSuffix);
            var newBuildConfigId = _client.BuildConfigs.GenerateUniqueBuildConfigId(sourceBuild.BuildConfig.ProjectId, newName).Result;

            var newBuildConfigXml = _buildConfigXmlClient.ReadAsOf(sourceBuild.BuildConfig.ProjectId, sourceBuild.BuildConfig.Id, sourceBuild.StartDate);

            var clonedBuildConfigXml = newBuildConfigXml.CopyBuildConfiguration(newBuildConfigId, newName);

            _originalBuildConfigIdToCloneMap.Add(sourceBuild.BuildConfig.Id, clonedBuildConfigXml);

            clonedBuildConfigXml.DeleteAllSnapshotDependencies();
            clonedBuildConfigXml.FreezeAllArtifactDependencies(sourceBuild);
            clonedBuildConfigXml.FreezeParameters(sourceBuild.Properties.Property);
            clonedBuildConfigXml.SetParameterValue(ParameterName.ClonedFromBuildId, sourceBuild.Id.ToString());
            clonedBuildConfigXml.SetParameterValue(ParameterName.BuildConfigChainId, _targetBuildChainId);
            clonedBuildConfigXml.SetParameterValue(ParameterName.BranchName, _newBranchName);

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
            private readonly CloneChildBuildConfigUseCase _receiver;
            private readonly Build _sourceBuild;

            public CloneBuildConfigCommand(CloneChildBuildConfigUseCase receiver, Build sourceBuild)
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
            private readonly CloneChildBuildConfigUseCase _receiver;
            private readonly Lazy<IBuildConfigXml> _swapOn;
            private readonly string _swapTo;
            private readonly string _swapFrom;

            public SwapDependencyCommand(CloneChildBuildConfigUseCase receiver, Lazy<IBuildConfigXml> swapOn, string swapTo, string swapFrom)
            {
                _receiver = receiver;
                _swapOn = swapOn;
                _swapTo = swapTo;
                _swapFrom = swapFrom;
            }

            public void Execute()
            {
                _receiver.SwapDependenciesToClone(_swapOn, _swapTo, _swapFrom);
            }

            public override string ToString()
            {
                return $"Swap dependencies on {_swapOn.Value.BuildConfigId}: {_swapFrom} => {_swapTo}";
            }

        }

        private interface ICommand
        {
            void Execute();
        }
    }
}