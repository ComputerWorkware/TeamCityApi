using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TeamCityApi.Clients;
using TeamCityApi.Domain;
using TeamCityApi.Logging;
using TeamCityApi.Model;
using TeamCityApi.Util;

namespace TeamCityApi.Helpers.Git
{
    public interface IVcsRootHelper
    {
        /// <summary>
        /// Clones, branches, pushes cleans up local folder
        /// </summary>
        /// <param name="buildId"></param>
        /// <param name="branchName"></param>
        /// <param name="newBuildConfigId">Used to update dependencies.config</param>
        /// <returns></returns>
        Task CloneAndBranchAndPushAndDeleteLocalFolder(long buildId, string branchName, string newBuildConfigId);
    }

    public class VcsRootHelper : IVcsRootHelper
    {
        private static readonly ILog Log = LogProvider.GetLogger(typeof(VcsRootHelper));

        private readonly ITeamCityClient _client;
        private readonly IGitRepositoryFactory _gitRepositoryFactory;

        public VcsRootHelper(ITeamCityClient client, IGitRepositoryFactory gitRepositoryFactory)
        {
            _client = client;
            _gitRepositoryFactory = gitRepositoryFactory;
        }

        public async Task<VcsCommit> GetCommitInformationByBuildId(long buildId)
        {
            Log.Info(string.Format("Get Commit Information for Build: {0}",buildId));
            Build build = await _client.Builds.ById(buildId);

            if (!build.Revisions.Any())
            {
                Log.Debug("Build doesn't have any VCS data");
                return null;
            }

            BuildConfig currentBuildConfig = await _client.BuildConfigs.GetByConfigurationId(build.BuildConfig.Id);

            Log.Debug("Build Loaded from TeamCity");

            string commitSha = build.Revisions.First().Version;

            Log.Debug(string.Format("Commit SHA from first Revision: {0}",commitSha));

            //use VCS Root from the current state of Build Config, instead of the old version, as it could have been moved to a different repository
            var vcsRootId = currentBuildConfig.VcsRootEntries.VcsRootEntry.First().VcsRoot.Id;

            Log.Debug(string.Format("Get VCSRoot by Id: {0}", vcsRootId));
            VcsRoot vcsRoot = await _client.VcsRoots.ById(vcsRootId);

            Log.Debug(string.Format("VCSRoot: {0}",vcsRoot));
            //build configs don't have resolved system parameters, so manually inject it as a workaround.
            //Another way to get resolved system parameters is to use ResultingProperties API call for the latest build of the config
            currentBuildConfig.Parameters.Property.Add(new Property
            {
                Name = ParameterName.SystemTeamcityProjectName,
                Value = currentBuildConfig.Project.Name
            });
            VcsCommit commit = new VcsCommit(vcsRoot, currentBuildConfig.Parameters.Property, commitSha);

            return commit;

        }
        
        public async Task CloneAndBranchAndPushAndDeleteLocalFolder(long buildId, string branchName, string newBuildConfigId)
        {
            VcsCommit commit = await GetCommitInformationByBuildId(buildId);

            if (commit == null)
            {
                Log.Info("Could not find commit for build. Skipping creation of branch step.");
                return;
            }

            IGitRepository gitRepository = _gitRepositoryFactory.Clone(commit);
            if (gitRepository == null)
                throw new Exception("Unable to Clone Git Repository and create branch");

            if (gitRepository.AddBranch(branchName, commit.CommitSha))
            {
                gitRepository.CheckoutBranch(branchName);
                UpdateDependencyConfig(gitRepository, newBuildConfigId);
                gitRepository.Push(branchName);
            }

            gitRepository.DeleteFolder();
        }

        private void UpdateDependencyConfig(IGitRepository gitRepository, string newBuildConfigId)
        {
            var dependenciesConfigFileName = "dependencies.config";
            var dependenciesConfigPath = Path.Combine(gitRepository.TempClonePath, dependenciesConfigFileName);
            if (!System.IO.File.Exists(dependenciesConfigPath))
            {
                Log.Debug($"dependencies.config at {dependenciesConfigPath} is not found. Skipping BuildConfigId change.");
                return;
            }

            var jsonString = System.IO.File.ReadAllText(dependenciesConfigPath);

            var config = Json.Deserialize<DependencyConfig>(jsonString);
            config.BuildConfigId = newBuildConfigId;

            System.IO.File.WriteAllText(dependenciesConfigPath, JsonConvert.SerializeObject(config, Formatting.Indented));

            gitRepository.StageAndCommit(new List<string> { dependenciesConfigFileName }, $"Change BuildConfigId in dependencies.config to {newBuildConfigId}");

            Log.Debug($"Changed BuildConfigId in dependencies.config to {newBuildConfigId}");
        }

        public static string ToValidGitBranchName(string input)
        {
            input = input.Replace(" ", "-");
            input = new Regex("[^a-zA-Z0-9-.]").Replace(input, "");
            return input;
        }

    }
}
