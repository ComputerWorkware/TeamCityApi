﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

        private DependencyChain _dependencyChain;
        private string _newNameSuffix;
        private string _newBranchName;
        private string _targetBuildChainId;
        private BuildConfig _sourceBuildConfig;
        private BuildConfig _targetRootBuildConfig;
        private readonly Dictionary<string, BuildConfig> _clones = new Dictionary<string, BuildConfig>();

        private bool _simulate;

        public CloneChildBuildConfigUseCase(ITeamCityClient client, IVcsRootHelper vcsRootHelper)
        {
            _client = client;
            _vcsRootHelper = vcsRootHelper;
        }

        public async Task Execute(string sourceBuildConfigId, string targetRootBuildConfigId, bool simulate)
        {
            Log.Info($"Clone Child Build Config. sourceBuildConfigId: {sourceBuildConfigId}, targetRootBuildConfigId: {targetRootBuildConfigId}");

            await Init(sourceBuildConfigId, targetRootBuildConfigId, simulate);

            var buildConfigsToClone = GetBuildsToClone();

            foreach (var b in buildConfigsToClone)
            {
                Log.Info($"==== Branch {b.BuildConfig.Id} from Build #{b.Build.Number} (id: {b.Build.Id}) ====");
                if (!_simulate)
                {
                    await _vcsRootHelper.CloneAndBranchAndPushAndDeleteLocalFolder(b.Build.Id, _newBranchName);
                }
            }

            var cloneBuildConfigCommands = GetCloneBuildConfigsCommands(buildConfigsToClone.ToList());
            await Task.WhenAll(cloneBuildConfigCommands.Select(c =>
            {
                Log.Info($"==== {c} ====");
                if (_simulate)
                    return Task.FromResult(0);
                else
                    return c.Execute();
            }));

            var swapDependencyCommands = GetSwapDependenciesCommands(buildConfigsToClone);
            await Task.WhenAll(swapDependencyCommands.Select(c =>
            {
                Log.Info($"==== {c} ====");
                if (_simulate)
                    return Task.FromResult(0);
                else
                    return c.Execute();
            }));
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

            if (!_dependencyChain.Contains(_sourceBuildConfig))
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

        private HashSet<CombinedDependency> GetBuildsToClone()
        {
            var sourceComdinedDependency = _dependencyChain.First(d => d.BuildConfig.Equals(_sourceBuildConfig));
            var buildsToClone = _dependencyChain.FindAllParents(sourceComdinedDependency);
            buildsToClone.Remove(new CombinedDependency(_targetRootBuildConfig));
            buildsToClone.Add(sourceComdinedDependency);
            buildsToClone.RemoveWhere(d => d.IsCloned);

            return buildsToClone;
        }

        private IEnumerable<CloneBuildConfigCommand> GetCloneBuildConfigsCommands(IEnumerable<CombinedDependency> buildConfigsToClone)
        {
            return buildConfigsToClone.Select(bc => new CloneBuildConfigCommand(this, bc.Build));
        }

        private IEnumerable<SwapDependencyCommand> GetSwapDependenciesCommands(IEnumerable<CombinedDependency> buildConfigsToClone)
        {
            var swapDependencyCommands = new List<SwapDependencyCommand>();
            foreach (var buildConfigToClone in buildConfigsToClone)
            {
                var parentBuildConfigs = _dependencyChain.GetParents(buildConfigToClone);

                foreach (var parentBuildConfig in parentBuildConfigs)
                {
                    var targetBuildConfig = buildConfigsToClone.Contains(parentBuildConfig) ? GetCloneOf(parentBuildConfig.BuildConfig) : parentBuildConfig.BuildConfig;

                    swapDependencyCommands.Add(new SwapDependencyCommand(this, targetBuildConfig, GetCloneOf(buildConfigToClone.BuildConfig), buildConfigToClone.BuildConfig.Id));
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
            await _client.BuildConfigs.SetParameterValue(newBuildConfig, ParameterName.ClonedFromBuildId, sourceBuild.Id.ToString());
            await _client.BuildConfigs.SetParameterValue(newBuildConfig, ParameterName.BuildConfigChainId, _targetBuildChainId);
            await _client.BuildConfigs.SetParameterValue(newBuildConfig, ParameterName.BranchName, _newBranchName);

            return newBuildConfig;
        }

        public async Task SwapDependencies(BuildConfig targetBuildConfig, BuildConfig buildConfigToSwapTo, string buildConfigIdToSwapFrom)
        {
            //Log.DebugFormat("SwapDependenciesToPreviouslyClonedBuildConfig(targetBuildConfig: {0}, previouslyClonedBuildConfig: {1}, previouslyClonedBuildConfigFromBuild: {2})", targetBuildConfig, previouslyClonedBuildConfig, previouslyClonedFromBuildConfigId);

            var artifactDependencyToSwap = targetBuildConfig.ArtifactDependencies.FirstOrDefault(a => a.SourceBuildConfig.Id == buildConfigIdToSwapFrom);

            if (artifactDependencyToSwap == null)
                throw new Exception(
                    $"Cannot find targetBuildConfig.ArtifactDependencies by SourceBuildConfig.Id == {buildConfigIdToSwapFrom}. " +
                    $"Available SourceBuildConfig.Ids are: {String.Join(", ", targetBuildConfig.ArtifactDependencies.Select(ad => ad.SourceBuildConfig.Id))}");

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
                return $"Clone {_sourceBuild.BuildConfig.Id} from Build #{_sourceBuild.Number}";
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
                return $"Swap dependencies on {_targetBuildConfig.Id}: {_buildConfigIdToSwapFrom} => {_buildConfigToSwapTo.Id}";
            }

        }

        private interface ICommand
        {
            Task Execute();
        }
    }
}