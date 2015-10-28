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

        public async Task<BuildConfig> Execute(string sourceBuildId, string newNameSuffix)
        {
            Log.InfoFormat("Clone Root Build Config. sourceBuildId: {0}, newNameSuffix: {1}", sourceBuildId, newNameSuffix);

            var sourceBuild = await _client.Builds.ById(sourceBuildId);

            await EnsureUniqueSuffixProvided(sourceBuild, newNameSuffix);

            var gitRepository = await _vcsRootHelper.CloneAndBranch(sourceBuildId, newNameSuffix);
            if (gitRepository==null)
                throw new Exception("Unable to Clone Git Repository and create branch");

            if (_vcsRootHelper.PushAndDeleteLocalFolder(gitRepository, newNameSuffix) == false)
            {
                throw new Exception("Unable to Push and remove temporary repository folder.");
            }

            return await CopyBuildConfigurationFromBuild(sourceBuild, newNameSuffix);
        }

        private async Task<BuildConfig> CopyBuildConfigurationFromBuild(Build sourceBuild, string newNameSuffix)
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
            await _client.BuildConfigs.SetParameterValue(newBuildConfig, ParameterName.ClonedFromBuildId, sourceBuild.Id);
            await _client.BuildConfigs.SetParameterValue(newBuildConfig, ParameterName.BuildConfigChainId, Guid.NewGuid().ToString());

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