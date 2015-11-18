using System;
using System.Linq;
using System.Threading.Tasks;
using TeamCityApi.Clients;
using TeamCityApi.Domain;
using TeamCityApi.Helpers.Git;
using TeamCityApi.Logging;

namespace TeamCityApi.UseCases
{
    public class CloneRootBuildConfigUseCase
    {
        private static readonly ILog Log = LogProvider.GetLogger(typeof(CloneRootBuildConfigUseCase));

        private readonly ITeamCityClient _client;
        private readonly IBuildConfigXmlClient _buildConfigXmlClient;
        private readonly IVcsRootHelper _vcsRootHelper;

        public CloneRootBuildConfigUseCase(ITeamCityClient client, IBuildConfigXmlClient buildConfigXmlClient, IVcsRootHelper vcsRootHelper)
        {
            _client = client;
            _vcsRootHelper = vcsRootHelper;
            _buildConfigXmlClient = buildConfigXmlClient;
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

        private async Task CopyBuildConfigurationFromBuild(Build sourceBuild, string newNameSuffix, string branchName)
        {
            var newName = BuildConfig.NewName(sourceBuild.BuildConfig.Name, newNameSuffix);
            var newBuildConfigId = await _client.BuildConfigs.GenerateUniqueBuildConfigId(sourceBuild.BuildConfig.ProjectId, newName);

            var buildConfigXml = _buildConfigXmlClient.ReadAsOf(sourceBuild.BuildConfig.ProjectId, sourceBuild.BuildConfig.Id, sourceBuild.StartDate);
            var clonedBuildConfigXml = buildConfigXml.CopyBuildConfiguration(newBuildConfigId, newName);

            clonedBuildConfigXml.DeleteAllSnapshotDependencies();
            clonedBuildConfigXml.FreezeAllArtifactDependencies(sourceBuild);
            clonedBuildConfigXml.FreezeParameters(sourceBuild.Properties.Property);
            clonedBuildConfigXml.SetParameterValue(ParameterName.CloneNameSuffix, newNameSuffix);
            clonedBuildConfigXml.SetParameterValue(ParameterName.ClonedFromBuildId, sourceBuild.Id.ToString());
            clonedBuildConfigXml.SetParameterValue(ParameterName.BuildConfigChainId, Guid.NewGuid().ToString());
            clonedBuildConfigXml.SetParameterValue(ParameterName.BranchName, branchName);

            _buildConfigXmlClient.EndSetOfChanges();
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