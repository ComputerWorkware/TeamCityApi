using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TeamCityApi.Clients;
using TeamCityApi.Domain;
using TeamCityApi.Logging;

namespace TeamCityApi.Helpers.Git
{
    public interface IVcsRootHelper
    {
        Task CloneAndBranchAndPushAndDeleteLocalFolder(long buildId, string branchName);
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
        
        public async Task CloneAndBranchAndPushAndDeleteLocalFolder(long buildId, string branchName)
        {
            VcsCommit commit = await GetCommitInformationByBuildId(buildId);

            IGitRepository gitRepository = _gitRepositoryFactory.Clone(commit);
            if (gitRepository == null)
                throw new Exception("Unable to Clone Git Repository and create branch");

            if (gitRepository.AddBranch(branchName, commit.CommitSha))
            {
                gitRepository.Push(branchName);
            }

            gitRepository.DeleteFolder();
        }

        public static string ToValidGitBranchName(string input)
        {
            input = input.Replace(" ", "-");
            input = new Regex("[^a-zA-Z0-9-]").Replace(input, "");
            return input;
        }

    }
}
