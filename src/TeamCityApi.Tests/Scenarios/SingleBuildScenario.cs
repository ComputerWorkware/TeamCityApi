using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml;
using NSubstitute;
using Ploeh.AutoFixture;
using TeamCityApi.Clients;
using TeamCityApi.Domain;
using TeamCityApi.Locators;
using TeamCityApi.Tests.Helpers;

namespace TeamCityApi.Tests.Scenarios
{
    public class SingleBuildScenario
    {
        public Build Build { get; set; }
        public BuildConfig BuildConfig { get; set; }
        public BuildConfigXml BuildConfigXml { get; set; }
        public Project Project { get; set; }

        public SingleBuildScenario(
            IFixture fixture,
            ITeamCityClient client,
            IBuildConfigXmlClient buildConfigXmlClient,
            long buildId,
            string buildConfigId = null,
            string buildConfigName = null,
            List<ScenarioDependency> dependencies = null,
            Properties buildParameters = null)
        {
            var projectId = fixture.Create<string>();
            buildConfigId = buildConfigId ?? fixture.Create<string>();

            BuildConfig = fixture.Build<BuildConfig>()
                .WithId(buildConfigId)
                .WithProjectId(projectId)
                .WithName(buildConfigName ?? fixture.Create<string>())
                .WithParameters(buildParameters ?? fixture.Create<Properties>())
                .WithDependencies(dependencies?.Select(d => d.AsDependencyDefinition()).ToArray() ??
                                  fixture.CreateMany<DependencyDefinition>().ToArray())
                .Create();

            BuildConfigXml = new BuildConfigXmlGenerator(buildConfigXmlClient)
                .WithProjectId(projectId)
                .WithId(buildConfigId)
                .WithName(buildConfigName ?? fixture.Create<string>())
                .WithParameters(buildParameters ?? fixture.Create<Properties>())
				.WithDependencies(dependencies?.Select(d => d.AsDependencyDefinition()).ToArray() ??
								  fixture.CreateMany<DependencyDefinition>().ToArray())
                .Create();

            Build = fixture.Build<Build>()
                .WithId(buildId)
				.WithDependencies(dependencies?.Select(d => d.AsDependency()).ToArray() ??
								  fixture.CreateMany<Dependency>().ToArray())
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

            buildConfigXmlClient
                .Read(BuildConfigXml.ProjectId, BuildConfigXml.BuildConfigId)
                .Returns(BuildConfigXml);

            buildConfigXmlClient
                .ReadAsOf(BuildConfigXml.ProjectId, BuildConfigXml.BuildConfigId, Arg.Any<DateTime>())
                .Returns(BuildConfigXml);

            var clonedBuildConfig = BuildConfig.CloneViaJson();
            clonedBuildConfig.Id = BuildConfig.Id + "_Clone";
            clonedBuildConfig.Name = buildConfigName + Consts.SuffixSeparator + "Clone";

            client.BuildConfigs.GenerateUniqueBuildConfigId(projectId, clonedBuildConfig.Name)
                .Returns(Task.FromResult(clonedBuildConfig.Id));
        }

	    public ScenarioDependency AsArtifactSameChainDependency()
	    {
			return new ScenarioDependency(this, DependencyType.ArtifactSameChain);
	    }

		public ScenarioDependency AsArtifactFixedBuildDependency()
		{
			return new ScenarioDependency(this, DependencyType.ArtifactFixedBuild);
		}
	}
}