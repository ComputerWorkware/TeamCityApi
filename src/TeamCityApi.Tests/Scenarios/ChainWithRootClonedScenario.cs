using System.Collections.Generic;
using System.Threading.Tasks;
using NSubstitute;
using Ploeh.AutoFixture;
using TeamCityApi.Clients;
using TeamCityApi.Domain;
using TeamCityApi.Tests.Helpers;

namespace TeamCityApi.Tests.Scenarios
{
    public class ChainWithRootClonedScenario
    {
        //                                                  Suite Cloned
        // Component A ──────────── App A ── Installer A ── Suite
        //  └───── Component B ──── App B ── Installer B ────┘
        // Component C───┘

        public SingleBuildScenario SuiteCloned { get; set; }
        public SingleBuildScenario Suite { get; set; }
        public SingleBuildScenario InstallerA { get; set; }
        public SingleBuildScenario InstallerB { get; set; }
        public SingleBuildScenario AppA { get; set; }
        public SingleBuildScenario AppB { get; set; }
        public SingleBuildScenario ComponentA { get; set; }
        public SingleBuildScenario ComponentB { get; set; }
        public SingleBuildScenario ComponentC { get; set; }

        public ChainWithRootClonedScenario(IFixture fixture, ITeamCityClient client, IBuildConfigXmlClient buildConfigXmlClient)
        {
            ComponentA = new SingleBuildScenario(fixture, client, buildConfigXmlClient, 311, "ComponentA_Trunk", "ComponentA", new List<ScenarioDependency>());

			ComponentB = new SingleBuildScenario(fixture, client, buildConfigXmlClient, 310, "ComponentB_Trunk", "ComponentB", new List<ScenarioDependency>{
	            ComponentA.AsArtifactSameChainDependency()
			});

			ComponentC = new SingleBuildScenario(fixture, client, buildConfigXmlClient, 309, "ComponentC_Trunk", "ComponentC", new List<ScenarioDependency>());

            AppA = new SingleBuildScenario(fixture, client, buildConfigXmlClient, 313, "AppA_Trunk", "AppA", new List<ScenarioDependency>{
				ComponentA.AsArtifactSameChainDependency()
			});

            AppB = new SingleBuildScenario(fixture, client, buildConfigXmlClient, 312, "AppB_Trunk", "AppB", new List<ScenarioDependency>{
				ComponentB.AsArtifactSameChainDependency(),
				ComponentC.AsArtifactSameChainDependency()
			});

			InstallerA = new SingleBuildScenario(fixture, client, buildConfigXmlClient, 315, "InstallerA_Trunk", "InstallerA", new List<ScenarioDependency>{
				AppA.AsArtifactSameChainDependency()
			});

			InstallerB = new SingleBuildScenario(fixture, client, buildConfigXmlClient, 314, "InstallerB_Trunk", "InstallerB", new List<ScenarioDependency>{
				AppB.AsArtifactSameChainDependency()
			});

			Suite = new SingleBuildScenario(fixture, client, buildConfigXmlClient, 316, "Suite_Trunk", "Suite", new List<ScenarioDependency>{
				InstallerA.AsArtifactSameChainDependency(),
				InstallerB.AsArtifactSameChainDependency(),
			});

			SuiteCloned = new SingleBuildScenario(fixture, client, buildConfigXmlClient, 400, "Suite_TrunkCloned", "SuiteCloned", new List<ScenarioDependency>
				{
					InstallerA.AsArtifactFixedBuildDependency(),
					InstallerB.AsArtifactFixedBuildDependency(),
				},
				new Properties() {
                    Property = new PropertyList()
                    {
                        new Property(ParameterName.BuildConfigChainId, fixture.Create<string>()),
                        new Property(ParameterName.CloneNameSuffix, "Clone")
                    }
                }
            );

            client.Builds
                .ByBuildLocator(locator => locator.WithSnapshotDependencyFrom(AppA.Build.Id))
                .Returns(Task.FromResult(new List<BuildSummary>() {(BuildSummary)AppA.Build, (BuildSummary)InstallerA.Build, (BuildSummary)Suite.Build }));
        }
    }
}