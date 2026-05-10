using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NSubstitute;
using TeamCityApi.Domain;
using TeamCityApi.Locators;
using TeamCityApi.Tests.Helpers;
using TeamCityApi.UseCases;
using Xunit;
using Xunit.Extensions;

namespace TeamCityApi.Tests.UseCases
{
    public class ProjectInventoryUseCaseTests
    {
        public class Execute
        {
            [Theory]
            [AutoNSubstituteData]
            public void should_generate_markdown_for_each_build_in_the_snapshot_chain(ITeamCityClient client)
            {
                var rootBuild = CreateBuild(100, "RootBuild", "master", "Team / Root Project", "http://teamcity/viewType.html?buildTypeId=RootBuild");
                var dependencyBuild = CreateBuild(200, "DependencyBuild", "trunk", "Team / Dependency Project", "http://teamcity/viewType.html?buildTypeId=DependencyBuild");

                client.Builds.ByBuildLocator(Arg.Any<Action<BuildLocator>>()).Returns(Task.FromResult(new List<BuildSummary>
                {
                    new BuildSummary { Id = 200 },
                    new BuildSummary { Id = 100 },
                    new BuildSummary { Id = 100 }
                }));

                client.Builds.ById(100).Returns(Task.FromResult(rootBuild));
                client.Builds.ById(200).Returns(Task.FromResult(dependencyBuild));

                client.Builds.GetResultingProperties(100).Returns(Task.FromResult(CreateProperties("[{\"name\":\"VOAPI.sln\",\"type\":\"Solution\",\"version\":\"12.00\"},{\"name\":\"VOAPI|Client.csproj\",\"type\":\".NET\",\"version\":\"net10.0\"},{\"name\":\"VOAPI.Domain.Tests.csproj\",\"type\":\".NET\",\"version\":\"net48\"}]")));
                client.Builds.GetResultingProperties(200).Returns(Task.FromResult(CreateProperties("[{\"name\":\"Dependency.csproj\",\"type\":\".NET\",\"version\":\"net48\"}]")));

                var sut = new ProjectInventoryUseCase(client);

                var markdown = sut.Execute(100).GetAwaiter().GetResult();

                var expected = string.Join(Environment.NewLine, new[]
                {
                    "| Solution | Project | Type | Version |",
                    "| --- | --- | --- | --- |",
                    "| [Dependency Project](http://teamcity/viewType.html?buildTypeId=DependencyBuild) | Dependency.csproj | .NET | net48 |",
                    "| [Root Project](http://teamcity/viewType.html?buildTypeId=RootBuild) | VOAPI\\|Client.csproj | .NET | net10.0 |",
                    string.Empty,
                    "| Type | Version | Count |",
                    "| --- | --- | --- |",
                    "| .NET | net10.0 | 1 |",
                    "| .NET | net48 | 1 |",
                    string.Empty
                });

                Assert.Equal(expected, markdown);
                client.Builds.Received(1).ById(100);
                client.Builds.Received(1).ById(200);
            }

            [Theory]
            [AutoNSubstituteData]
            public void should_render_na_row_when_project_inventory_is_missing(ITeamCityClient client)
            {
                client.Builds.ByBuildLocator(Arg.Any<Action<BuildLocator>>()).Returns(Task.FromResult(new List<BuildSummary>
                {
                    new BuildSummary { Id = 100 }
                }));
                client.Builds.ById(100).Returns(Task.FromResult(CreateBuild(100, "RootBuild", "master", "Team / Root Project", "http://teamcity/viewType.html?buildTypeId=RootBuild")));
                client.Builds.GetResultingProperties(100).Returns(Task.FromResult(new Properties { Property = new PropertyList() }));

                var sut = new ProjectInventoryUseCase(client);

                var markdown = sut.Execute(100).GetAwaiter().GetResult();

                var expected = string.Join(Environment.NewLine, new[]
                {
                    "| Solution | Project | Type | Version |",
                    "| --- | --- | --- | --- |",
                    "| [Root Project](http://teamcity/viewType.html?buildTypeId=RootBuild) | N/A | N/A | N/A |",
                    string.Empty,
                    "| Type | Version | Count |",
                    "| --- | --- | --- |",
                    "| N/A | N/A | 1 |",
                    string.Empty
                });

                Assert.Equal(expected, markdown);
            }

            [Theory]
            [AutoNSubstituteData]
            public void should_render_single_inventory_object(ITeamCityClient client)
            {
                client.Builds.ByBuildLocator(Arg.Any<Action<BuildLocator>>()).Returns(Task.FromResult(new List<BuildSummary>
                {
                    new BuildSummary { Id = 100 }
                }));
                client.Builds.ById(100).Returns(Task.FromResult(CreateBuild(100, "RootBuild", "master", "Team / Root Project", "http://teamcity/viewType.html?buildTypeId=RootBuild")));
                client.Builds.GetResultingProperties(100).Returns(Task.FromResult(CreateProperties("{\"name\":\"VOAPI.csproj\",\"type\":\".NET\",\"version\":\"net10.0\"}")));

                var sut = new ProjectInventoryUseCase(client);

                var markdown = sut.Execute(100).GetAwaiter().GetResult();

                var expected = string.Join(Environment.NewLine, new[]
                {
                    "| Solution | Project | Type | Version |",
                    "| --- | --- | --- | --- |",
                    "| [Root Project](http://teamcity/viewType.html?buildTypeId=RootBuild) | VOAPI.csproj | .NET | net10.0 |",
                    string.Empty,
                    "| Type | Version | Count |",
                    "| --- | --- | --- |",
                    "| .NET | net10.0 | 1 |",
                    string.Empty
                });

                Assert.Equal(expected, markdown);
            }

            [Theory]
            [AutoNSubstituteData]
            public void should_render_na_row_when_project_inventory_is_invalid_json(ITeamCityClient client)
            {
                client.Builds.ByBuildLocator(Arg.Any<Action<BuildLocator>>()).Returns(Task.FromResult(new List<BuildSummary>
                {
                    new BuildSummary { Id = 100 }
                }));
                client.Builds.ById(100).Returns(Task.FromResult(CreateBuild(100, "RootBuild", "master", "Team / Root Project", "http://teamcity/viewType.html?buildTypeId=RootBuild")));
                client.Builds.GetResultingProperties(100).Returns(Task.FromResult(CreateProperties("not-json")));

                var sut = new ProjectInventoryUseCase(client);

                var markdown = sut.Execute(100).GetAwaiter().GetResult();

                var expected = string.Join(Environment.NewLine, new[]
                {
                    "| Solution | Project | Type | Version |",
                    "| --- | --- | --- | --- |",
                    "| [Root Project](http://teamcity/viewType.html?buildTypeId=RootBuild) | N/A | N/A | N/A |",
                    string.Empty,
                    "| Type | Version | Count |",
                    "| --- | --- | --- |",
                    "| N/A | N/A | 1 |",
                    string.Empty
                });

                Assert.Equal(expected, markdown);
            }

            [Theory]
            [AutoNSubstituteData]
            public void should_render_na_for_unresolved_inventory_fields(ITeamCityClient client)
            {
                client.Builds.ByBuildLocator(Arg.Any<Action<BuildLocator>>()).Returns(Task.FromResult(new List<BuildSummary>
                {
                    new BuildSummary { Id = 100 }
                }));
                client.Builds.ById(100).Returns(Task.FromResult(CreateBuild(100, "RootBuild", "master", "Team / Root Project", "http://teamcity/viewType.html?buildTypeId=RootBuild")));
                client.Builds.GetResultingProperties(100).Returns(Task.FromResult(CreateProperties("[{\"name\":\"\",\"type\":\".NET\",\"version\":null}]")));

                var sut = new ProjectInventoryUseCase(client);

                var markdown = sut.Execute(100).GetAwaiter().GetResult();

                var expected = string.Join(Environment.NewLine, new[]
                {
                    "| Solution | Project | Type | Version |",
                    "| --- | --- | --- | --- |",
                    "| [Root Project](http://teamcity/viewType.html?buildTypeId=RootBuild) | N/A | .NET | N/A |",
                    string.Empty,
                    "| Type | Version | Count |",
                    "| --- | --- | --- |",
                    "| .NET | N/A | 1 |",
                    string.Empty
                });

                Assert.Equal(expected, markdown);
            }

            [Theory]
            [AutoNSubstituteData]
            public void should_not_render_rows_filtered_as_solutions_or_tests(ITeamCityClient client)
            {
                client.Builds.ByBuildLocator(Arg.Any<Action<BuildLocator>>()).Returns(Task.FromResult(new List<BuildSummary>
                {
                    new BuildSummary { Id = 100 }
                }));
                client.Builds.ById(100).Returns(Task.FromResult(CreateBuild(100, "RootBuild", "master", "Team / Root Project", "http://teamcity/viewType.html?buildTypeId=RootBuild")));
                client.Builds.GetResultingProperties(100).Returns(Task.FromResult(CreateProperties("[{\"name\":\"Root.sln\",\"type\":\"Solution\",\"version\":\"12.00\"},{\"name\":\"Root.Tests.csproj\",\"type\":\".NET\",\"version\":\"net48\"}]")));

                var sut = new ProjectInventoryUseCase(client);

                var markdown = sut.Execute(100).GetAwaiter().GetResult();

                var expected = string.Join(Environment.NewLine, new[]
                {
                    "| Solution | Project | Type | Version |",
                    "| --- | --- | --- | --- |",
                    string.Empty,
                    "| Type | Version | Count |",
                    "| --- | --- | --- |",
                    string.Empty
                });

                Assert.Equal(expected, markdown);
            }

            [Theory]
            [AutoNSubstituteData]
            public void should_group_similar_versions_in_summary_table(ITeamCityClient client)
            {
                client.Builds.ByBuildLocator(Arg.Any<Action<BuildLocator>>()).Returns(Task.FromResult(new List<BuildSummary>
                {
                    new BuildSummary { Id = 100 }
                }));
                client.Builds.ById(100).Returns(Task.FromResult(CreateBuild(100, "RootBuild", "master", "Team / Root Project", "http://teamcity/viewType.html?buildTypeId=RootBuild")));
                client.Builds.GetResultingProperties(100).Returns(Task.FromResult(CreateProperties("[{\"name\":\"A.csproj\",\"type\":\".NET\",\"version\":\"v4.8\"},{\"name\":\"B.csproj\",\"type\":\".NET\",\"version\":\"v4.8.1\"},{\"name\":\"C.csproj\",\"type\":\".NET\",\"version\":\"net48\"}]")));

                var sut = new ProjectInventoryUseCase(client);

                var markdown = sut.Execute(100).GetAwaiter().GetResult();

                var expected = string.Join(Environment.NewLine, new[]
                {
                    "| Solution | Project | Type | Version |",
                    "| --- | --- | --- | --- |",
                    "| [Root Project](http://teamcity/viewType.html?buildTypeId=RootBuild) | A.csproj | .NET | v4.8 |",
                    "| [Root Project](http://teamcity/viewType.html?buildTypeId=RootBuild) | B.csproj | .NET | v4.8.1 |",
                    "| [Root Project](http://teamcity/viewType.html?buildTypeId=RootBuild) | C.csproj | .NET | net48 |",
                    string.Empty,
                    "| Type | Version | Count |",
                    "| --- | --- | --- |",
                    "| .NET | net48 | 3 |",
                    string.Empty
                });

                Assert.Equal(expected, markdown);
            }
        }

        private static Build CreateBuild(long id, string buildTypeId, string buildConfigName, string projectName, string webUrl)
        {
            return new Build
            {
                Id = id,
                BuildTypeId = buildTypeId,
                BuildConfig = new BuildConfigSummary
                {
                    Id = buildTypeId,
                    Name = buildConfigName,
                    ProjectName = projectName,
                    WebUrl = webUrl
                },
                WebUrl = webUrl
            };
        }

        private static Properties CreateProperties(string projectInventoryJson)
        {
            return new Properties
            {
                Property = new PropertyList
                {
                    new Property(ParameterName.ProjectInventory, projectInventoryJson)
                }
            };
        }
    }
}
