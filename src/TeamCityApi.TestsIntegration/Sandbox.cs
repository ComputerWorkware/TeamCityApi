using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TeamCityApi.Clients;
using TeamCityApi.Domain;
using TeamCityApi.Helpers;
using TeamCityApi.Helpers.Git;
using TeamCityApi.Helpers.Graphs;
using TeamCityApi.Locators;
using TeamCityApi.UseCases;
using Xunit;
using Xunit.Extensions;
using File = TeamCityApi.Domain.File;

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
        public void Build_ByLocatorWithFields()
        {
            var buildClient = CreateBuildClient();

            List<Build> builds = buildClient.ByBuildLocatorWithFields(bl => 
                bl
                    .SinceBuildId(877898)
                    .WithBuildConfiguration(bc => bc.WithId("Suite_Master"))
                    .WithCount(10000)
                ,
                bf => bf
                    .WithLong()
                    .WithBuildFields(b => b
                        .WithId()
                        .WithNumber()
                        .WithStatus()
                        .WithFinishDate()
                        .WithChangesFields(cs => cs
                            .WithChangeFields(c => c
                                .WithId()
                                .WithComment()
                                .WithUserFields(u => u
                                    .WithName()
                                    .WithUsername()
                                )
                                .WithFilesFields(fs => fs
                                    .WithFileFields(f => f
                                        .WithChangeType()
                                        .WithFile())
                                )
                            )
                        )
                    )).Result;
        }

        [Fact]
        public void Build_ById()
        {
            var buildClient = CreateBuildClient();

            Build build = buildClient.ById(456).Result;
        }

        [Fact]
        public void Build_GetResultingProperties()
        {
            var buildClient = CreateBuildClient();

            var properties = buildClient.GetResultingProperties(456).Result;
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
                var client = CreateChangeClient();
                var changes = client.GetAll().Result;
            }
            
            [Fact]
            public void GetForBuild()
            {
                var client = CreateChangeClient();
                var changes = client.GetForBuild(987200).Result;
            }

            [Fact]
            public void GetForBuildConfig()
            {
                var client = CreateChangeClient();
                var changes = client.GetForBuildConfig("Installers_VitalObjects_VitalObjectsSuite_Master").Result;
            }

            [Fact]
            public void GetForBuildConfigSinceChange()
            {
                var client = CreateChangeClient();
                var changes = client.GetForBuildConfig("Installers_VitalObjects_VitalObjectsSuite_Master", 85201).Result;
            }

            [Fact]
            public void GetForBuildConfigPending()
            {
                var client = CreateChangeClient();
                var changes = client.GetForBuildConfig("Installers_VitalObjects_VitalObjectsSuite_Master", true).Result;
            }

            [Fact]
            public void GetById()
            {
                var client = CreateChangeClient();
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
                var compareBuildsUseCase = new CompareBuildsUseCase(new TeamCityClient("teamcityserver:8080", "user", "pass"));
                compareBuildsUseCase.Execute(178416, 180701, false, false).Wait();
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
                var teamCityClient = CreateTeamCityClient();
                var gitRepositoryFactory = CreateGitRepositoryFactory();
                var gitLabClientFactory = CreateGitLabClientFactory();
                var buildConfigXmlClient = new BuildConfigXmlClient(teamCityClient, gitRepositoryFactory);
                var vcsRootHelper = new VcsRootHelper(teamCityClient, gitRepositoryFactory, gitLabClientFactory);

                var cloneRootBuildConfigUseCase = new CloneRootBuildConfigUseCase(teamCityClient, buildConfigXmlClient, vcsRootHelper);

                cloneRootBuildConfigUseCase.Execute(781, "TestingDependenciesConfig12", false).Wait();
            }
        }

        public class CloneChildBuildConfig
        {
            [Fact]
            public void Should_clone_child_build_config()
            {
                var teamCityClient = CreateTeamCityClient();
                var gitRepositoryFactory = CreateGitRepositoryFactory();
                var gitLabClientFactory = CreateGitLabClientFactory();
                var buildConfigXmlClient = new BuildConfigXmlClient(teamCityClient, gitRepositoryFactory);
                var vcsRootHelper = new VcsRootHelper(teamCityClient, gitRepositoryFactory, gitLabClientFactory);

                var cloneChildBuildConfigUseCase = new CloneChildBuildConfigUseCase(CreateTeamCityClient(), vcsRootHelper, buildConfigXmlClient);

                cloneChildBuildConfigUseCase.Execute("Installers_Sunlife_PaymentCollections_Trunk", "Installers_Sunlife_VitalObjectsSuite_trunkTestingDependenciesConfig12", false).Wait();
            }
        }

        public class DeepCloneBuildConfig
        {
            [Fact]
            public void Should_deep_clone_build_config()
            {
                var teamCityClient = CreateTeamCityClient();
                var gitRepositoryFactory = CreateGitRepositoryFactory();
                var gitLabClientFactory = CreateGitLabClientFactory();
                var buildConfigXmlClient = new BuildConfigXmlClient(teamCityClient, gitRepositoryFactory);
                var vcsRootHelper = new VcsRootHelper(teamCityClient, gitRepositoryFactory, gitLabClientFactory);
                var deleteClonedBuildChainUseCase = new DeepCloneBuildConfigUseCase(teamCityClient, vcsRootHelper, buildConfigXmlClient);

                deleteClonedBuildChainUseCase.Execute(sourceBuildId: 522, simulate:false, newNameSuffix: "Deep Clone Test 8").Wait();
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

        public class ShowBuildChainUseCase
        {
            [Theory]
            public void Should_show_build_chain()
            {
                var showBuildChainUseCase = new UseCases.ShowBuildChainUseCase(CreateTeamCityClient());

                showBuildChainUseCase.Execute("Installers_Sunlife_VitalObjectsSuite_TrunkKris",
                    UseCases.ShowBuildChainUseCase.BuildChainView.List,
                    UseCases.ShowBuildChainUseCase.BuildChainFilter.Cloned).Wait();
            }
        }

        private static ChangeClient CreateChangeClient()
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

        private static List<GitCredential> CreateGitCredentials()
        {
            return new List<GitCredential>
            {
                new GitCredential
                {
                    HostName = "*",
                    UserName = "user",
                    Password = "pass"
                }
            };
        }

        private static IGitRepositoryFactory CreateGitRepositoryFactory()
        {
            return new GitRepositoryFactory(CreateGitCredentials());
        }

        private static GitLabSettings CreateGitLabSettings()
        {
            return new GitLabSettings() {GitLabUsername = "user", GitLabPassword = "pass", GitLabUri = "http://gitlabserver/"};
        }

        private static IGitLabClientFactory CreateGitLabClientFactory()
        {
            return new GitLabClientFactory(CreateGitLabSettings());
        }
    }
}