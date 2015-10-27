using System.Threading.Tasks;
using NSubstitute;
using Ploeh.AutoFixture;
using TeamCityApi.Domain;
using TeamCityApi.Tests.Helpers;

namespace TeamCityApi.Tests.Scenarios
{
    public class SingleBuildScenario
    {
        public Build Build { get; set; }
        public BuildConfig BuildConfig { get; set; }
        public Project Project { get; set; }

        public SingleBuildScenario(IFixture fixture, ITeamCityClient client, string buildId)
        {
            BuildConfig = fixture.Create<BuildConfig>();

            Build = fixture.Build<Build>()
                    .WithId(buildId)
                    .WithBuildConfigSummary(BuildConfig)
                    .Create();

            Project = fixture.Build<Project>()
                .WithId(Build.BuildConfig.ProjectId)
                .WithBuildConfigSummary(BuildConfig)
                .Create();

            client.Builds
                .ById(buildId)
                .Returns(Task.FromResult(Build));

            client.Projects
                .GetById(Project.Id)
                .Returns(Task.FromResult(Project));

            client.BuildConfigs
                .GetByConfigurationId(BuildConfig.Id)
                .Returns(Task.FromResult(BuildConfig));

            client.BuildConfigs
                .CopyBuildConfiguration(Project.Id, Arg.Any<string>(), BuildConfig.Id, Arg.Any<bool>(), Arg.Any<bool>())
                .Returns(Task.FromResult(fixture.Create<BuildConfig>()));
        }
    }
}