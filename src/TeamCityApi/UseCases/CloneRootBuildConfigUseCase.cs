using System;
using System.Threading.Tasks;
using TeamCityApi.Clients;
using TeamCityApi.Domain;
using TeamCityApi.Locators;
using TeamCityApi.Logging;

namespace TeamCityApi.UseCases
{
    public class CloneRootBuildConfigUseCase
    {
        private static readonly ILog Log = LogProvider.GetLogger(typeof(CloneRootBuildConfigUseCase));

        private readonly ITeamCityClient _client;

        public CloneRootBuildConfigUseCase(ITeamCityClient client)
        {
            _client = client;
        }

        public async Task<BuildConfig> Execute(string sourceBuildId, string newNameSuffix)
        {
            Log.InfoFormat("CloneRootBuildConfigUseCase.Execute(sourceBuildId: {0}, newNameSuffix: {1})", sourceBuildId, newNameSuffix);

            var build = await _client.Builds.ById(sourceBuildId);

            return await CopyBuildConfigurationFromBuild(build, newNameSuffix);
        }

        private async Task<BuildConfig> CopyBuildConfigurationFromBuild(Build sourceBuild, string newNameSuffix)
        {
            var newBuildConfig = await _client.BuildConfigs.CopyBuildConfiguration(
                l => l.WithId(sourceBuild.BuildConfig.ProjectId),
                BuildConfig.NewName(sourceBuild.BuildConfig.Name, newNameSuffix),
                l => l.WithId(sourceBuild.BuildConfig.Id)
            );

            await _client.BuildConfigs.DeleteAllSnapshotDependencies(newBuildConfig);
            await _client.BuildConfigs.FreezeAllArtifactDependencies(newBuildConfig, sourceBuild);
            await _client.BuildConfigs.FreezeParameters(newBuildConfig, newBuildConfig.Parameters.Property, sourceBuild.Properties.Property);
            await _client.BuildConfigs.SetParameterValue(newBuildConfig, ParameterName.CloneNameSuffix, newNameSuffix);
            await _client.BuildConfigs.SetParameterValue(newBuildConfig, ParameterName.ClonedFromBuildId, sourceBuild.Id);
            await _client.BuildConfigs.SetParameterValue(newBuildConfig, ParameterName.BuildConfigChainId, Guid.NewGuid().ToString());

            return newBuildConfig;
        }
    }
}