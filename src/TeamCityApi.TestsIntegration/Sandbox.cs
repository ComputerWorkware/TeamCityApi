using System;
using System.Collections.Generic;
using System.Linq;
using TeamCityApi.Clients;
using TeamCityApi.Domain;
using TeamCityApi.Helpers;
using TeamCityApi.Helpers.Graphs;
using TeamCityApi.UseCases;
using Xunit;

namespace TeamCityApi.TestsIntegration
{
    public class Sandbox
    {
        [Fact]
        public void Build_ByLocator()
        {
            var buildClient = CreateBuildClient();

            List<BuildSummary> buildSummaries = buildClient.ByBuildLocator(x => x.SinceDate(new DateTime(2014, 05, 11))).Result;
        }

        [Fact]
        public void Build_ById()
        {
            var buildClient = CreateBuildClient();

            Build build = buildClient.ById(456).Result;
        }

        [Fact]
        public void Build_Artifacts()
        {
            var buildClient = CreateBuildClient();

            Build build = buildClient.ById(45).Result;

            List<File> files = build.ArtifactsReference.GetFiles().Result;
        }

        [Fact]
        public void GetFiles()
        {
            BuildClient buildClient = CreateBuildClient();

            List<File> result = buildClient.GetFiles(48).Result;

            List<File> files = result.First().GetChildren().Result;
        }

        public class BuildConfig
        {
            [Fact]
            public void ById()
            {
                var client = CreateBuildConfigClient();
                var buildConfig = client.GetByConfigurationId("Installers_Sunlife_VitalObjectsSuite_Trunk").Result;
            }

            [Fact]
            public void SetParameterValue()
            {
                var client = CreateBuildConfigClient();
                client.SetParameterValue(
                    l => l.WithId("Installers_Sunlife_VitalObjectsSuite_TrunkOct13Release"),
                    "MinorVersion",
                    "77").Wait();
            }

            [Fact]
            public void CopyBuildConfigurationFromBuildId()
            {
                var client = CreateBuildConfigClient();
                //var createdBuildConfig = client.CloneRootBuildConfig("250", "Do not touch!").Result;
            }
        }

        public class Project
        {
            [Fact]
            public void ById()
            {
                var client = CreateProjectClient();
                var project = client.GetById("id29_BIFLoader").Result;
            }
        }

        public class Change
        {
            [Fact]
            public void GetAll()
            {
                var client = CreateChnageClient();
                var changes = client.GetAll().Result;
            }

            [Fact]
            public void GetById()
            {
                var client = CreateChnageClient();
                var change = client.GetById("4").Result;
            }
        }

        public class Dependencies
        {
            [Fact]
            public void Snapshot()
            {
                var client = CreateBuildConfigClient();
                var summaries = client.GetAll().Result;
                //var buildConfig = client.GetByConfigurationId("FooCore_Master").Result;
                var buildConfig = summaries.First(x => x.Id == "FooCore_Master");
                var dependencyDefinition = new DependencyDefinition
                {
                    Id = buildConfig.Id,
                    Type = "snapshot_dependency",
                    Properties = new Properties
                    {
                        Property = new PropertyList
                        {
                            new Property() { Name = "run-build-if-dependency-failed", Value = BuildContinuationMode.MakeFailedToStart.ToString() },
                            new Property() { Name = "take-successful-builds-only", Value = "true" },
                            //new Property() { Name = "run-build-on-the-same-agent", Value = "true" },
                            //new Property() { Name = "take-started-build-with-same-revisions", Value = "true" },
                        }
                    }, 
                    SourceBuildConfig = buildConfig
                };
                client.CreateDependency("foo_service_Master", dependencyDefinition).Wait();
            }

            [Fact]
            public void DeleteSnapshotDependency()
            {
                var client = CreateBuildConfigClient();
                client.DeleteSnapshotDependency("Installers_Sunlife_ReinsuredPremiumCollections_Trunk", "Sunlife_ReinsuredCollections_Trunk").Wait();
            }

            [Fact]
            public void DeleteAllSnapshotDependencies()
            {
                var client = CreateBuildConfigClient();
                var buildConfiguration = client.GetByConfigurationId("Installers_Sunlife_VitalObjectsSuite_TrunkKrisTest").Result;
                client.DeleteAllSnapshotDependencies(buildConfiguration).Wait();
            }

            [Fact]
            public void FreezeAllArtifactDependencies()
            {
                var buildClient = CreateBuildClient();
                var build = buildClient.ById(250).Result;

                var buildConfigurationClient = CreateBuildConfigClient();
                var buildConfiguration = buildConfigurationClient.GetByConfigurationId("Installers_Sunlife_VitalObjectsSuite_TrunkDoNotTouch").Result;

                buildConfigurationClient.FreezeAllArtifactDependencies(buildConfiguration, build).Wait();
            }
            
            [Fact]
            public void DeleteBuildConfig()
            {
                var client = CreateBuildConfigClient();
                client.DeleteBuildConfig("Sunlife_PaymentCollections_TrunkKrisTest").Wait();
            }

            [Fact]
            public void GetAllSnapshotDependencies()
            {
                var client = CreateBuildConfigClient();
                client.GetAllSnapshotDependencies("366").Wait();
            }

            [Fact]
            public void Snapshot_()
            {
                var client = CreateBuildConfigClient();
                var dependency = new CreateSnapshotDependency("foo_service_Master","FooCore_Master");
                client.CreateSnapshotDependency(dependency).Wait();
            }

            [Fact]
            public void CompareBuilds()
            {
                var compareBuildsUseCase = new CompareBuildsUseCase(new TeamCityClient("devciserver:8080", "ciserver", "ciserver"));
                compareBuildsUseCase.Execute(178416, 180701, false).Wait();
            }
        }

        public class BuildChain
        {
            [Fact]
            public void Create()
            {
                var client = CreateBuildClient();
                var build = client.ById(186).Result;

                var buildChain = new Helpers.BuildChain(client, build);

                Assert.Equal(9, buildChain.Count);
            }

            [Fact]
            public void GetParents()
            {
                var client = CreateBuildClient();
                var build = client.ById(186).Result;
                var childBuild = client.ById(116).Result;

                var buildChain = new Helpers.BuildChain(client, build);
                var parentBuilds = buildChain.GetParents(childBuild).ToList();

                Assert.Equal(new List<long> { 181, 132, 124 }, parentBuilds.Select(b => b.Id));
            }
        }

        public class BuildConfigChain
        {
            [Fact]
            public void Create()
            {
                var client = CreateBuildConfigClient();
                var buildConfig = client.GetByConfigurationId("Installers_Sunlife_VitalObjectsSuite_Trunk").Result;

                var buildConfigChain = new Helpers.BuildConfigChain(client, buildConfig);

                Assert.Equal(9, buildConfigChain.Count);
            }

            [Fact]
            public void FindAllParents()
            {
                var client = CreateBuildConfigClient();
                var buildConfig = client.GetByConfigurationId("Installers_Sunlife_VitalObjectsSuite_Trunk").Result;
                var buildConfigChain = new Helpers.BuildConfigChain(client, buildConfig);
                var child = client.GetByConfigurationId("Sunlife_CwiVoAccountingAddins_Trunk").Result;

                var allParents = buildConfigChain.FindAllParents(child);

                Assert.Equal(6, allParents.Count);
            }
        }

        public class DependencyChain
        {
            [Fact]
            public void DependencyChainCreate()
            {
                var client = CreateTeamCityClient();
                var buildConfig = client.BuildConfigs.GetByConfigurationId("Installers_Sunlife_VitalObjectsSuite_TrunkAlexTest").Result;

                var dependencyChain = new Helpers.DependencyChain(client, buildConfig);

                Assert.Equal(9, dependencyChain.Count);
            }
        }

        public class CloneRootBuildConfig
        {
            [Fact]
            public void Should_clone_root_build_config()
            {
                IVcsRootHelper rootHelper = null;  // NULL object and method will fail unless created or substituted
                var cloneRootBuildConfigUseCase = new CloneRootBuildConfigUseCase(CreateTeamCityClient(), rootHelper);

                cloneRootBuildConfigUseCase.Execute(268, "Release Oct 13", false).Wait();
            }
        }

        public class CloneChildBuildConfig
        {
            [Fact]
            public void Should_clone_child_build_config()
            {
                var cloneChildBuildConfigUseCase = new CloneChildBuildConfigUseCase(CreateTeamCityClient(), null);

                cloneChildBuildConfigUseCase.Execute("Sunlife_CwiVoAccountingAddins_Trunk", "Installers_Sunlife_VitalObjectsSuite_TrunkAlexTest", true).Wait();
            }
        }

        public class DeleteClonedBuildChain
        {
            [Fact]
            public void Should_delete_build_chain()
            {
                var deleteClonedBuildChainUseCase = new DeleteClonedBuildChainUseCase(CreateTeamCityClient());

                deleteClonedBuildChainUseCase.Execute("Installers_Sunlife_VitalObjectsSuite_TrunkKrisTest", simulate:true).Wait();
            }
        }

        private static ChangeClient CreateChnageClient()
        {
            var http = CreateHttpClientWrapper();
            var client = new ChangeClient(http);
            return client;
        }

        private static ProjectClient CreateProjectClient()
        {
            var http = CreateHttpClientWrapper();
            var projectClient = new ProjectClient(http);
            return projectClient;
        }

        private static BuildConfigClient CreateBuildConfigClient()
        {
            var http = CreateHttpClientWrapper();
            var buildConfigClient = new BuildConfigClient(http);
            return buildConfigClient;
        }

        private static HttpClientWrapper CreateHttpClientWrapper()
        {
            var http = new HttpClientWrapper("teamcitytest:8080", "teamcity", "teamcity");
            return http;
        }

        private static BuildClient CreateBuildClient()
        {
            var http = new HttpClientWrapper("teamcitytest:8080", "teamcity", "teamcity");
            var buildClient = new BuildClient(http);
            return buildClient;
        }

        private static ITeamCityClient CreateTeamCityClient()
        {
            var http = new HttpClientWrapper("teamcitytest:8080", "teamcity", "teamcity");
            var client = new TeamCityClient(http);
            return client;
        }
    }
}