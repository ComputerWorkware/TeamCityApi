using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NGitLab.Models;
using TeamCityApi.Clients;
using TeamCityApi.Domain;
using TeamCityApi.Helpers.Git;
using TeamCityApi.Logging;
using TeamCityApi.Model;
using TeamCityApi.Util;

namespace TeamCityApi.Helpers
{
    public interface IVcsRootHelper
    {
        /// <summary>
        /// Clones, branches, pushes, cleans up local folder
        /// </summary>
        /// <param name="buildId"></param>
        /// <param name="branchName"></param>
        /// <param name="newBuildConfigId">Used to update dependencies.config</param>
        /// <returns></returns>
        Task BranchUsingLocalGit(long buildId, string branchName);

        Task BranchUsingGitLabApi(long buildId, string branchName);
    }

    public class VcsRootHelper : IVcsRootHelper
    {
        private static readonly ILog Log = LogProvider.GetLogger(typeof(VcsRootHelper));

        private readonly ITeamCityClient _client;
        private readonly IGitRepositoryFactory _gitRepositoryFactory;
        private readonly IGitLabClientFactory _gitLabClientFactory;

        public VcsRootHelper(ITeamCityClient client, IGitRepositoryFactory gitRepositoryFactory, IGitLabClientFactory gitLabClientFactory)
        {
            _client = client;
            _gitRepositoryFactory = gitRepositoryFactory;
            _gitLabClientFactory = gitLabClientFactory;
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
        
        public async Task BranchUsingLocalGit(long buildId, string branchName)
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
                gitRepository.Push(branchName);
            }

            gitRepository.DeleteFolder();
        }

        public async Task BranchUsingGitLabApi(long buildId, string branchName)
        {
            VcsCommit commit = await GetCommitInformationByBuildId(buildId);

            if (commit == null)
            {
                Log.Info("Could not find commit for build. Skipping creation of branch step.");
                return;
            }

            var gitLabClient = _gitLabClientFactory.GetGitLabClient();

            var project = gitLabClient.Projects.Get(commit.RepositoryNameWithNamespace);

            var repo = gitLabClient.GetRepository(project.Id);

            var existingBranches = repo.Branches.All();

            if (!existingBranches.Any(b => string.Equals(b.Name, branchName, StringComparison.InvariantCultureIgnoreCase)))
            {
                repo.Branches.Create(new BranchCreate()
                {
                    Name = branchName,
                    Ref = commit.CommitSha
                });
            }
        }

        public static string ToValidGitBranchName(string input)
        {
            input = input.Replace(" ", "-");
            input = new Regex("[^a-zA-Z0-9-.]").Replace(input, "");
            return input;
        }

    }
}
