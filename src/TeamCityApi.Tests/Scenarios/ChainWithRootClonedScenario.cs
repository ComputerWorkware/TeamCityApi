using System.Collections.Generic;
using System.Threading.Tasks;
using NSubstitute;
using Ploeh.AutoFixture;
using TeamCityApi.Domain;
using TeamCityApi.Tests.Helpers;

namespace TeamCityApi.Tests.Scenarios
{
    public class ChainWithRootClonedScenario
    {
        //                                            Suite Cloned
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
        

        public ChainWithRootClonedScenario(IFixture fixture, ITeamCityClient client)
        {
            ComponentA = new SingleBuildScenario(fixture, client, 311, "ComponentA_Trunk", "ComponentA", new List<DependencyDefinition>());
            ComponentB = new SingleBuildScenario(fixture, client, 310, "ComponentB_Trunk", "ComponentB", 
                new List<DependencyDefinition>
                {
                    BuildConfigDependencyGenerator.ArtifactSameChain(ComponentA.BuildConfig)
                },
                new List<Dependency>()
                {
                    BuildDependencyGenerator.Artifact(ComponentA.Build)
                }
            );
            ComponentC = new SingleBuildScenario(fixture, client, 309, "ComponentC_Trunk", "ComponentC", new List<DependencyDefinition>());
            
            AppA = new SingleBuildScenario(fixture, client, 313, "AppA_Trunk", "AppA", 
                new List<DependencyDefinition>
                {
                    BuildConfigDependencyGenerator.ArtifactSameChain(ComponentA.BuildConfig)
                },
                new List<Dependency>()
                {
                    BuildDependencyGenerator.Artifact(ComponentA.Build)
                }
            );
            AppB = new SingleBuildScenario(fixture, client, 312, "AppB_Trunk", "AppB", 
                new List<DependencyDefinition>
                {
                    BuildConfigDependencyGenerator.ArtifactSameChain(ComponentB.BuildConfig),
                    BuildConfigDependencyGenerator.ArtifactSameChain(ComponentC.BuildConfig)
                },
                new List<Dependency>()
                {
                    BuildDependencyGenerator.Artifact(ComponentB.Build),
                    BuildDependencyGenerator.Artifact(ComponentC.Build)
                }
            );

            InstallerA = new SingleBuildScenario(fixture, client, 315, "InstallerA_Trunk", "InstallerA", 
                new List<DependencyDefinition>
                {
                    BuildConfigDependencyGenerator.ArtifactSameChain(AppA.BuildConfig),
                },
                new List<Dependency>()
                {
                    BuildDependencyGenerator.Artifact(AppA.Build)
                }
            );
            InstallerB = new SingleBuildScenario(fixture, client, 314, "InstallerB_Trunk", "InstallerB", 
                new List<DependencyDefinition>
                {
                    BuildConfigDependencyGenerator.ArtifactSameChain(AppB.BuildConfig),
                },
                new List<Dependency>()
                {
                    BuildDependencyGenerator.Artifact(AppB.Build)
                }
            );

            Suite = new SingleBuildScenario(fixture, client, 316, "Suite_Trunk", "Suite", 
                new List<DependencyDefinition>
                {
                    BuildConfigDependencyGenerator.ArtifactSameChain(InstallerA.BuildConfig),
                    BuildConfigDependencyGenerator.ArtifactSameChain(InstallerB.BuildConfig)
                },
                new List<Dependency>()
                {
                    BuildDependencyGenerator.Artifact(InstallerA.Build),
                    BuildDependencyGenerator.Artifact(InstallerB.Build)
                }
            );
            SuiteCloned = new SingleBuildScenario(fixture, client, 400, "Suite_TrunkCloned", "SuiteCloned", 
                new List<DependencyDefinition>
                {
                    BuildConfigDependencyGenerator.ArtifactFixedBuild(InstallerA.Build),
                    BuildConfigDependencyGenerator.ArtifactFixedBuild(InstallerB.Build)
                },
                new List<Dependency>()
                {
                    BuildDependencyGenerator.Artifact(InstallerA.Build),
                    BuildDependencyGenerator.Artifact(InstallerB.Build)
                },
                fixture.Create<string>()
            );

            client.Builds
                .ByBuildLocator(locator => locator.WithSnapshotDependencyFrom(AppA.Build.Id))
                .Returns(Task.FromResult(new List<BuildSummary>() {(BuildSummary)AppA.Build, (BuildSummary)InstallerA.Build, (BuildSummary)Suite.Build }));
        }
    }
}