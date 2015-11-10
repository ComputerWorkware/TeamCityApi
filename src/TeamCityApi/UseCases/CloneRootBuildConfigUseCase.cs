using System;
using System.Linq;
using System.Threading.Tasks;
using TeamCityApi.Clients;
using TeamCityApi.Domain;
using TeamCityApi.Helpers;
using TeamCityApi.Locators;
using TeamCityApi.Logging;

namespace TeamCityApi.UseCases
{
    public class CloneRootBuildConfigUseCase
    {
        private static readonly ILog Log = LogProvider.GetLogger(typeof(CloneRootBuildConfigUseCase));

        private readonly ITeamCityClient _client;
        private readonly IVcsRootHelper _vcsRootHelper;

        public CloneRootBuildConfigUseCase(ITeamCityClient client, IVcsRootHelper vcsRootHelper)
        {
            _client = client;
            _vcsRootHelper = vcsRootHelper;
        }

        public async Task Execute(long sourceBuildId, string newNameSuffix, bool simulate)
        {
            Log.InfoFormat("Clone Root Build Config. sourceBuildId: {0}, newNameSuffix: {1}", sourceBuildId, newNameSuffix);

            var sourceBuild = await _client.Builds.ById(sourceBuildId);

            await EnsureUniqueSuffixProvided(sourceBuild, newNameSuffix);

            var newBranchName = VcsRootHelper.ToValidGitBranchName(newNameSuffix);

            Log.InfoFormat("==== Branch {2} from Build #{1} (id: {0}) ====", sourceBuild.Id, sourceBuild.Number, sourceBuild.BuildConfig.Id);
            if (!simulate)
                await _vcsRootHelper.CloneAndBranchAndPushAndDeleteLocalFolder(sourceBuildId, newBranchName);

            Log.InfoFormat("==== Clone {2} from Build #{1} (id: {0}) ====", sourceBuild.Id, sourceBuild.Number, sourceBuild.BuildConfig.Id);
            if (!simulate)
                await CopyBuildConfigurationFromBuild(sourceBuild, newNameSuffix, newBranchName);
        }

        private async Task<BuildConfig> CopyBuildConfigurationFromBuild(Build sourceBuild, string newNameSuffix, string branchName)
        {
            var newBuildConfig = await _client.BuildConfigs.CopyBuildConfiguration(
                sourceBuild.BuildConfig.ProjectId,
                BuildConfig.NewName(sourceBuild.BuildConfig.Name, newNameSuffix),
                sourceBuild.BuildConfig.Id
            );

            await _client.BuildConfigs.DeleteAllSnapshotDependencies(newBuildConfig);
            await _client.BuildConfigs.FreezeAllArtifactDependencies(newBuildConfig, sourceBuild);
            await _client.BuildConfigs.FreezeParameters(newBuildConfig, newBuildConfig.Parameters.Property, sourceBuild.Properties.Property);
            await _client.BuildConfigs.SetParameterValue(newBuildConfig, ParameterName.CloneNameSuffix, newNameSuffix);
            await _client.BuildConfigs.SetParameterValue(newBuildConfig, ParameterName.ClonedFromBuildId, sourceBuild.Id.ToString());
            await _client.BuildConfigs.SetParameterValue(newBuildConfig, ParameterName.BuildConfigChainId, Guid.NewGuid().ToString());
            await _client.BuildConfigs.SetParameterValue(newBuildConfig, ParameterName.BranchName, branchName);

            return newBuildConfig;
        }

        private async Task EnsureUniqueSuffixProvided(Build sourceBuild, string newNameSuffix)
        {
            var targetProject = await _client.Projects.GetById(sourceBuild.BuildConfig.ProjectId);
            if (await ProjectHasBuildConfigWithSuffix(targetProject, newNameSuffix))
                throw new Exception(String.Format("There's already a Build Config with \"{1}\" suffix in Project \"{0}\". Provide unique suffix, because it will be used as a branch name in git.",
                    targetProject.Name, newNameSuffix));
        }

        private async Task<bool> ProjectHasBuildConfigWithSuffix(Project project, string suffix)
        {
            var getBuildConfigTasks = project.BuildConfigs.Select(bc => _client.BuildConfigs.GetByConfigurationId(bc.Id));
            var buildConfigs = await Task.WhenAll(getBuildConfigTasks);
            return buildConfigs.Any(bc => bc.Parameters[ParameterName.CloneNameSuffix].Value == suffix);
        }
    }
}