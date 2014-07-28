using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NSubstitute;
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.Xunit;
using TeamCityApi;
using TeamCityApi.Domain;
using TeamCityApi.Util;
using TeamCityConsole.Commands;
using TeamCityConsole.Model;
using TeamCityConsole.Options;
using TeamCityConsole.Utils;
using Xunit;
using Xunit.Extensions;
using TeamCityConsole.Tests.Helpers;

namespace TeamCityConsole.Tests.Commands
{
    public class ResolveDependencyCommandTests
    {
        public class Execute
        {
            [Theory]
            [AutoNSubstituteData]
            internal void Should_download_file_from_dependency(
                ITeamCityClient client,
                IFileDownloader downloader,
                IFileSystem fileSystem,
                string buildConfigId,
                IFixture fixture)
            {
                var dependencyDefinition = fixture.Build<DependencyDefinition>()
                    .WithPathRules("file.dll => assemblies")
                    .Create();

                var buildConfig = fixture.Build<BuildConfig>()
                    .WithId(buildConfigId)
                    .WithDependencies(dependencyDefinition)
                    .Create();

                client.BuildConfigs.GetByConfigurationId(buildConfigId).Returns(Task.FromResult(buildConfig));

                ConfigureDependency(client, dependencyDefinition, fixture);

                var command = new ResolveDependencyCommand(client, downloader, fileSystem);

                var options = fixture.Build<GetDependenciesOptions>()
                    .WithForce(buildConfigId)
                    .Create();

                command.Execute(options).Wait();

                downloader.Received().Download(options.OutputPath, Arg.Any<File>());
            }

            [Theory]
            [AutoNSubstituteData]
            internal void Should_download_each_dependency_in_the_path_rules(
                ITeamCityClient client,
                IFileDownloader downloader,
                IFileSystem fileSystem,
                string buildConfigId,
                IFixture fixture)
            {
                //2 files are defined in the path rules
                var dependencyDefinition = fixture.Build<DependencyDefinition>()
                    .WithPathRules("fileA.dll => assemblies"+Environment.NewLine+"fileB.dll=>assemblies")
                    .Create();

                var buildConfig = fixture.Build<BuildConfig>()
                    .WithId(buildConfigId)
                    .WithDependencies(dependencyDefinition)
                    .Create();

                client.BuildConfigs.GetByConfigurationId(buildConfigId).Returns(Task.FromResult(buildConfig));

                ConfigureDependency(client, dependencyDefinition, fixture);

                var command = new ResolveDependencyCommand(client, downloader, fileSystem);

                var options = fixture.Build<GetDependenciesOptions>()
                    .WithForce(buildConfigId)
                    .Create();

                command.Execute(options).Wait();

                //ensures 2 files were downloaded
                downloader.Received(2).Download(options.OutputPath, Arg.Any<File>());
            }

            [Theory]
            [AutoNSubstituteData]
            internal void Should_resolve_dependencies_recursively(
                ITeamCityClient client,
                IFileDownloader downloader,
                IFileSystem fileSystem,
                string buildConfigId,
                IFixture fixture)
            {
                //A -> B -> C
                
                //configure A -> B
                var dependencyDefinitionB = fixture.Build<DependencyDefinition>()
                    .WithPathRules("fileB.dll => assemblies")
                    .Create();

                var buildConfig = fixture.Build<BuildConfig>()
                    .WithId(buildConfigId)
                    .WithDependencies(dependencyDefinitionB)
                    .Create();

                client.BuildConfigs.GetByConfigurationId(buildConfigId).Returns(Task.FromResult(buildConfig));

                BuildConfig buildConfigB = ConfigureDependency(client, dependencyDefinitionB, fixture);

                //configure B -> C
                var dependencyDefinitionC = fixture.Build<DependencyDefinition>()
                    .WithPathRules("fileC.dll => assemblies")
                    .Create();

                buildConfigB.ArtifactDependencies.Add(dependencyDefinitionC);

                ConfigureDependency(client, dependencyDefinitionC, fixture);

                var command = new ResolveDependencyCommand(client, downloader, fileSystem);

                var options = fixture.Build<GetDependenciesOptions>()
                    .WithForce(buildConfigId)
                    .Create();

                command.Execute(options).Wait();

                //ensures 2 files were downloaded
                downloader.Received().Download(options.OutputPath, Arg.Is<File>(file => file.Name == "fileB.dll"));
                downloader.Received().Download(options.OutputPath, Arg.Is<File>(file => file.Name == "fileC.dll"));
            }
        }

        private static BuildConfig ConfigureDependency(ITeamCityClient client, DependencyDefinition dependencyDefinition, IFixture fixture)
        {
            Build build = fixture.Build<Build>()
                .With(x => x.BuildTypeId, dependencyDefinition.SourceBuildConfig.Id)
                .Create();

            BuildConfig buildConfig = fixture.Build<BuildConfig>()
                    .WithId(dependencyDefinition.SourceBuildConfig.Id)
                    .WithNoDependencies()
                    .Create();

            client.BuildConfigs.GetByConfigurationId(dependencyDefinition.SourceBuildConfig.Id)
                .Returns(Task.FromResult(buildConfig));

            client.Builds.LastSuccessfulBuildFromConfig(dependencyDefinition.SourceBuildConfig.Id)
                .Returns(Task.FromResult(build));

            return buildConfig;
        }

        public class When_project_has_no_dependencies
        {
            [Theory]
            [AutoNSubstituteData]
            internal void Should_initialize_DependencyConfig_with_empty_BuildInfos_list(
                ITeamCityClient client, 
                IFileDownloader downloader, 
                IFileSystem fileSystem, 
                string buildConfigId, 
                IFixture fixture)
            {
                BuildConfig buildConfig = fixture.Build<BuildConfig>()
                    .WithId(buildConfigId)
                    .WithNoDependencies()
                    .Create();

                client.BuildConfigs.GetByConfigurationId(buildConfigId).Returns(Task.FromResult(buildConfig));

                DependencyConfig config = null;

                fileSystem.When(x => x.WriteAllTextToFile(Arg.Any<string>(), Arg.Any<string>()))
                    .Do(info =>
                    {
                        var content = info.Args()[1] as string;
                        config = Json.Deserialize<DependencyConfig>(content);
                    });

                var command = new ResolveDependencyCommand(client, downloader, fileSystem);

                var options = fixture.Build<GetDependenciesOptions>()
                    .WithForce(buildConfigId)
                    .Create();

                command.Execute(options).Wait();

                fileSystem.Received().WriteAllTextToFile(Arg.Any<string>(), Arg.Any<string>());

                Assert.Equal(buildConfigId, config.BuildConfigId);
                Assert.Empty(config.BuildInfos);
                Assert.Equal("assemblies", config.OutputPath);
            }
        }

        public class When_config_file_does_not_exist
        {
            [Theory]
            [AutoNSubstituteData]
            internal void Should_initialize_new_config_from_command_line_options(
                ITeamCityClient client,
                IFileDownloader downloader,
                IFileSystem fileSystem,
                GetDependenciesOptions options,
                string fileName)
            {
                fileSystem.FileExists(Arg.Any<string>()).Returns(false);
                options.Force = true;

                var command = new ResolveDependencyCommand(client, downloader, fileSystem);

                DependencyConfig config = command.LoadConfigFile(options, fileName);

                Assert.Equal(options.BuildConfigId, config.BuildConfigId);
                Assert.Equal(options.OutputPath, config.OutputPath);
                Assert.Empty(config.BuildInfos);
            }

            [Theory]
            [AutoNSubstituteData]
            internal void Should_default_output_folder_to_assemblies_if_not_specified(
                ITeamCityClient client,
                IFileDownloader downloader,
                IFileSystem fileSystem,
                string fileName)
            {
                fileSystem.FileExists(Arg.Any<string>()).Returns(false);
                var options = new GetDependenciesOptions {Force = true}; 

                var command = new ResolveDependencyCommand(client, downloader, fileSystem);

                DependencyConfig config = command.LoadConfigFile(options, fileName);

                Assert.Equal("assemblies", config.OutputPath);
            }

            [Theory]
            [AutoNSubstituteData]
            internal void Should_throw_exception_if_Force_option_not_set(
                ITeamCityClient client,
                IFileDownloader downloader,
                IFileSystem fileSystem,
                GetDependenciesOptions options,
                string fileName)
            {
                fileSystem.FileExists(Arg.Any<string>()).Returns(false);
                options.Force = false;

                var command = new ResolveDependencyCommand(client, downloader, fileSystem);

                Assert.Throws<Exception>(() => command.LoadConfigFile(options, fileName));
            }
        }

        public class When_config_file_exists
        {
            [Theory]
            [AutoNSubstituteData]
            internal void Load_options_from_file(
                ITeamCityClient client,
                IFileDownloader downloader,
                IFileSystem fileSystem,
                GetDependenciesOptions options,
                string fileName)
            {
                string json = @"
{
  ""BuildConfigId"": ""MyConfig"",
  ""BuildInfos"": [
    {
      ""Id"": ""6611"",
      ""Number"": ""5.0.10721.1"",
      ""BuildConfigId"": ""DependencyConfig1"",
      ""CommitHash"": ""28fbf97bda61e99ac414979811a1e7bc2605211b""
    },
    {
      ""Id"": ""6609"",
      ""Number"": ""5.0.10721.9"",
      ""BuildConfigId"": ""DependencyConfig1"",
      ""CommitHash"": ""e5a9aa677cc88ff48c42749a26ebac736535e87c""
    }
  ],
  ""OutputPath"": ""assemblies""
}";

                fileSystem.FileExists(Arg.Any<string>()).Returns(true);
                fileSystem.ReadAllTextFromFile(Arg.Any<string>()).Returns(json);

                var command = new ResolveDependencyCommand(client, downloader, fileSystem);

                DependencyConfig config = command.LoadConfigFile(options, fileName);

                Assert.Equal("MyConfig", config.BuildConfigId);
                Assert.Equal("assemblies", config.OutputPath);
                Assert.Equal(2, config.BuildInfos.Count);
            }
        }


    }
}