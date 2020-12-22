
using NGitLab;
using NGitLab.Models;
using System.Linq;
using System.Threading.Tasks;
using TeamCityApi.Helpers;
using TeamCityApi.Helpers.Git;
using TeamCityApi.Logging;

namespace TeamCityApi.UseCases
{
    public class DeleteGitBranchesInBuildChainUseCase
    {
        private static readonly ILog Log = LogProvider.GetLogger(typeof(DeleteGitBranchesInBuildChainUseCase));

        private readonly ITeamCityClient _client;
        private readonly IGitLabClientFactory _gitLabClientFactory;

        public DeleteGitBranchesInBuildChainUseCase(ITeamCityClient client, IGitLabClientFactory gitLabClientFactory)
        {
            _client = client;
            _gitLabClientFactory = gitLabClientFactory;
        }

        public async Task Execute(string buildConfigId, string branch, bool simulate = false)
        {
            Log.InfoFormat("Delete Git Branch in Build Chain.");

            var buildConfig = await _client.BuildConfigs.GetByConfigurationId(buildConfigId);
            var buildChainId = buildConfig.Parameters[ParameterName.BuildConfigChainId].Value;
            var buildConfigChain = new BuildConfigChain(_client.BuildConfigs, buildConfig);
            DeleteGitBranchesInBuildChain(buildConfigChain, buildChainId, branch, simulate);
        }

        private void DeleteGitBranchesInBuildChain(BuildConfigChain buildConfigChain, string buildChainId, string branch, bool simulate)
        {
            var gitLabClient = _gitLabClientFactory.GetGitLabClient();

            var buildConfigsInChain = buildConfigChain.Nodes
                .Where(node => node.Value.Parameters[ParameterName.BuildConfigChainId].Value == buildChainId)
                .Select(n => n.Value)
                .ToList();

            foreach (var buildConfig in buildConfigsInChain)
            {
                Log.InfoFormat("Processing BuildConfigId {0}", buildConfig.Id);

                var gitRepoPath = buildConfig.Parameters[ParameterName.GitRepoPath].Value;

                if (string.IsNullOrWhiteSpace(gitRepoPath))
                {
                    Log.WarnFormat("    {0} parameter is empty. Skipping...", ParameterName.GitRepoPath);
                    continue;
                }

                gitRepoPath = gitRepoPath.Replace("%system.teamcity.projectName%", buildConfig.Project.Name);

                Project project;
                try
                {
                    project = gitLabClient.Projects.Get(gitRepoPath);
                }
                catch (System.Exception)
                {
                    Log.WarnFormat("    Gitlab project {0} is not found. Skipping...", gitRepoPath);
                    continue;
                }

                IRepositoryClient repo;
                try
                {
                    repo = gitLabClient.GetRepository(project.Id);
                }
                catch (System.Exception)
                {
                    Log.WarnFormat("    repo for project id {0} is not found. Skipping...", project.Id);
                    continue;
                }

                Branch existingBranch;

                try
                {
                    existingBranch = repo.Branches.Get(branch);
                }
                catch (System.Exception)
                {
                    Log.WarnFormat("    branch {0} in the project {1} does not exist. Nothing to delete. Skipping...", branch, gitRepoPath);
                    continue;
                }

                if (simulate)
                {
                    Log.InfoFormat("    simulating deletion of {0} branch in the {1} repo", branch, gitRepoPath);
                }
                else
                {
                    var branchRemovalStatus = repo.Branches.Delete(branch);

                    if (branchRemovalStatus.Succeed)
                    {
                        Log.InfoFormat("    deleted {0} branch in the {1} repo", branch, gitRepoPath);
                    }
                    else
                    {
                        Log.ErrorFormat("    failed to delete {0} branch in the {1} repo", branch, gitRepoPath);
                    }
                }
            }
        }
    }
}