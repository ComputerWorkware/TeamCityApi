using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using NSubstitute;
using Ploeh.AutoFixture;
using TeamCityApi.Domain;
using TeamCityApi.Locators;
using TeamCityApi.Tests.Helpers;

namespace TeamCityApi.Tests.Scenarios
{
    public class SingleBuildScenario
    {
        public Build Build { get; set; }
        public BuildConfig BuildConfig { get; set; }
        public Project Project { get; set; }

        public SingleBuildScenario(
            IFixture fixture,
            ITeamCityClient client,
            long buildId,
            string buildConfigId = null,
            string buildConfigName = null,
            IEnumerable<DependencyDefinition> buildConfigDependencies = null,
            IEnumerable<Dependency> buildDependencies = null,
            string buildConfigChainId = null)
        {
            BuildConfig = fixture.Build<BuildConfig>()
                .WithId(buildConfigId ?? fixture.Create<string>())
                .WithName(buildConfigName ?? fixture.Create<string>())
                .WithBuildConfigChainIdParameter(buildConfigChainId)
                .WithDependencies(buildConfigDependencies?.ToArray() ??
                                  fixture.CreateMany<DependencyDefinition>().ToArray())
                .Create();

            Build = fixture.Build<Build>()
                .WithId(buildId)
                .WithDependencies(buildDependencies?.ToArray() ?? fixture.CreateMany<Dependency>().ToArray())
                .WithBuildConfigSummary(BuildConfig)
                .Create();

            Project = fixture.Build<Project>()
                .WithId(Build.BuildConfig.ProjectId)
                .WithBuildConfigSummary(BuildConfig)
                .Create();

            client.Builds
                .ById(buildId)
                .Returns(Task.FromResult(Build));

            client.Builds
                .ByNumber(Build.Number, BuildConfig.Id)
                .Returns(Task.FromResult(Build));
            
            client.Projects
                .GetById(Project.Id)
                .Returns(Task.FromResult(Project));

            client.BuildConfigs
                .GetByConfigurationId(BuildConfig.Id)
                .Returns(Task.FromResult(BuildConfig));

            var clonedBuildConfig = BuildConfig.CloneViaJson();
            clonedBuildConfig.Id = "Clone_of_" + BuildConfig.Id;
            clonedBuildConfig.Name = "Clone of " + BuildConfig.Name;

            client.BuildConfigs
                .CopyBuildConfiguration(Project.Id, Arg.Any<string>(), BuildConfig.Id, Arg.Any<bool>(), Arg.Any<bool>())
                .Returns(Task.FromResult(clonedBuildConfig));
        }
    }
}