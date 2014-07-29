using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using NSubstitute;
using NSubstitute.Core;
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
using File = TeamCityApi.Domain.File;

namespace TeamCityConsole.Tests.Commands
{
    public class ResolveDependencyCommandTests
    {
        public class Execute
        {
            [Theory]
            [AutoNSubstituteData]
            internal void Should_download_file_from_dependency(
                TestResolveDependencyCommand command,
                string buildConfigId,
                IFixture fixture)
            {
                var dependencyDefinition = fixture.Build<DependencyDefinition>()
                    .WithPathRules("file.dll => src/assemblies")
                    .Create();

                var buildConfig = fixture.Build<BuildConfig>()
                    .WithId(buildConfigId)
                    .WithDependencies(dependencyDefinition)
                    .Create();

                command.Client.BuildConfigs.GetByConfigurationId(buildConfigId).Returns(Task.FromResult(buildConfig));

                ConfigureDependency(command.Client, dependencyDefinition, fixture);

                var options = fixture.Build<GetDependenciesOptions>()
                    .With(x => x.BuildConfigId, buildConfigId)
                    .Without(x => x.ConfigFilePath)
                    .Create();

                command.Execute(options).Wait();

                command.Downloader.Received().Download("src\\assemblies", Arg.Any<File>());
            }

            [Theory]
            [AutoNSubstituteData]
            internal void Should_download_file_from_dependency_with_rule_destination_as_target_directory(
                TestResolveDependencyCommand command,
                string buildConfigId,
                IFixture fixture)
            {
                var dependencyDefinition = fixture.Build<DependencyDefinition>()
                    .WithPathRules("file.dll => src/assemblies")
                    .Create();

                var buildConfig = fixture.Build<BuildConfig>()
                    .WithId(buildConfigId)
                    .WithDependencies(dependencyDefinition)
                    .Create();

                command.Client.BuildConfigs.GetByConfigurationId(buildConfigId).Returns(Task.FromResult(buildConfig));

                ConfigureDependency(command.Client, dependencyDefinition, fixture);

                var options = fixture.Build<GetDependenciesOptions>()
                    .With(x => x.BuildConfigId, buildConfigId)
                    .Without(x => x.ConfigFilePath)
                    .Create();

                command.Execute(options).Wait();

                command.Downloader.Received().Download("src\\assemblies", Arg.Any<File>());
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
                fileSystem.FileExists(@"c:\projects\projectA\dependencies.config").Returns(true);

                fileSystem.GetWorkingDirectory().Returns(@"c:\projects\projectA\src");

                fileSystem.GetFullPath(@"src\assemblies").Returns(@"c:\projects\projectA\src\assemblies");

                //2 files are defined in the path rules
                var dependencyDefinition = fixture.Build<DependencyDefinition>()
                    .WithPathRules("fileA.dll => src/assemblies"+Environment.NewLine+"fileB.dll=>src/assemblies")
                    .Create();

                var buildConfig = fixture.Build<BuildConfig>()
                    .WithId(buildConfigId)
                    .WithDependencies(dependencyDefinition)
                    .Create();

                client.BuildConfigs.GetByConfigurationId(buildConfigId).Returns(Task.FromResult(buildConfig));

                ConfigureDependency(client, dependencyDefinition, fixture);

                var command = new ResolveDependencyCommand(client, downloader, fileSystem);

                var options = fixture.Build<GetDependenciesOptions>()
                    .With(x => x.BuildConfigId, buildConfigId)
                    .Without(x => x.ConfigFilePath)
                    .Create();

                command.Execute(options).Wait();

                //ensures 2 files were downloaded
                downloader.Received(2).Download("..\\src\\assemblies", Arg.Any<File>());
            }

            [Theory]
            [AutoNSubstituteData]
            internal void Should_resolve_dependencies_recursively(
                TestResolveDependencyCommand command,
                string buildConfigId,
                IFixture fixture)
            {
                //A -> B -> C
                
                //configure A -> B
                var dependencyDefinitionB = fixture.Build<DependencyDefinition>()
                    .WithPathRules("fileB.dll => src/assemblies")
                    .Create();

                var buildConfig = fixture.Build<BuildConfig>()
                    .WithId(buildConfigId)
                    .WithDependencies(dependencyDefinitionB)
                    .Create();

                command.Client.BuildConfigs.GetByConfigurationId(buildConfigId).Returns(Task.FromResult(buildConfig));

                BuildConfig buildConfigB = ConfigureDependency(command.Client, dependencyDefinitionB, fixture);

                //configure B -> C
                var dependencyDefinitionC = fixture.Build<DependencyDefinition>()
                    .WithPathRules("fileC.dll => src/assemblies")
                    .Create();

                buildConfigB.ArtifactDependencies.Add(dependencyDefinitionC);

                ConfigureDependency(command.Client, dependencyDefinitionC, fixture);

                var options = fixture.Build<GetDependenciesOptions>()
                    .With(x => x.BuildConfigId, buildConfigId)
                    .Without(x => x.ConfigFilePath)
                    .Create();

                command.Execute(options).Wait();

                //ensures 2 files were downloaded
                command.Downloader.Received().Download("src\\assemblies", Arg.Is<File>(file => file.Name == "fileB.dll"));
                command.Downloader.Received().Download("src\\assemblies", Arg.Is<File>(file => file.Name == "fileC.dll"));
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
                fileSystem.FileExists(Arg.Any<string>()).Returns(true);

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
                    .With(x => x.BuildConfigId, buildConfigId)
                    .Without(x => x.BuildConfigId)
                    .Create();

                command.Execute(options).Wait();

                fileSystem.Received().WriteAllTextToFile(Arg.Any<string>(), Arg.Any<string>());

                Assert.Equal(buildConfigId, config.BuildConfigId);
                Assert.Empty(config.BuildInfos);
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

                var command = new ResolveDependencyCommand(client, downloader, fileSystem);

                DependencyConfig config = command.LoadConfigFile(options, fileName);

                Assert.Equal(options.BuildConfigId, config.BuildConfigId);
                Assert.Empty(config.BuildInfos);
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
                options.BuildConfigId = null;

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
  ]
}";
                options.BuildConfigId = null;

                fileSystem.FileExists(Arg.Any<string>()).Returns(true);
                fileSystem.ReadAllTextFromFile(Arg.Any<string>()).Returns(json);

                var command = new ResolveDependencyCommand(client, downloader, fileSystem);

                DependencyConfig config = command.LoadConfigFile(options, fileName);

                Assert.Equal("MyConfig", config.BuildConfigId);
                Assert.Equal(2, config.BuildInfos.Count);
            }
        }

        public class LoadConfigFile
        {
            string _json = @"
{
  ""BuildConfigId"": ""MyConfig"",
  ""BuildInfos"": []
}";
            [Theory]
            [AutoNSubstituteData]
            public void Should_search_for_config_on_containing_folders(IFixture fixture,
                TestResolveDependencyCommand command)
            {
                string executingDir = @"c:\projects\projectA\src\";
                string projectDir = @"c:\projects\projectA\";
                string config = "dependencies.config";

                command.FileSystem.GetWorkingDirectory().Returns(executingDir);

                command.FileSystem.ReadAllTextFromFile(Arg.Any<string>()).Returns(_json);

                var options = fixture.Build<GetDependenciesOptions>()
                    .Without(x => x.ConfigFilePath)
                    .Without(x => x.BuildConfigId)
                    .Create();

                command.LoadConfigFile(options, "dependencies.config");

                command.FileSystem.Received().ReadAllTextFromFile(projectDir + config);
            }

            [Theory]
            [AutoNSubstituteData]
            public void Should_use_download_directory_relative_to_config_file_location(IFixture fixture,
                TestResolveDependencyCommand command)
            {
                string buildConfigId = "MyConfig";
                string executingDir = @"c:\projects\projectA\src\";
                string projectDir = @"c:\projects\projectA\";
                string config = "dependencies.config";

                command.FileSystem.GetWorkingDirectory().Returns(executingDir);
                command.FileSystem.GetFullPath(@"src\assemblies").Returns(projectDir + @"src\assemblies");

                command.FileSystem.FileExists(executingDir + config).Returns(false);
                command.FileSystem.FileExists(projectDir + config).Returns(true);

                command.FileSystem.ReadAllTextFromFile(projectDir + config).Returns(_json);

                var dependencyDefinition = fixture.Build<DependencyDefinition>()
                    .WithPathRules("file.dll => src/assemblies")
                    .Create();

                var buildConfig = fixture.Build<BuildConfig>()
                    .WithId(buildConfigId)
                    .WithDependencies(dependencyDefinition)
                    .Create();

                command.Client.BuildConfigs.GetByConfigurationId(buildConfigId).Returns(Task.FromResult(buildConfig));

                ConfigureDependency(command.Client, dependencyDefinition, fixture);

                var options = fixture.Build<GetDependenciesOptions>()
                    .Without(x => x.ConfigFilePath)
                    .Without(x => x.BuildConfigId)
                    .Create();

                command.Execute(options).Wait();

                command.Downloader.Received().Download(@"..\src\assemblies", Arg.Any<File>());
            }

            [Theory]
            [AutoNSubstituteData]
            public void Should_throw_exception_when_config_not_found(IFixture fixture,
                TestResolveDependencyCommand command)
            {
                string executingDir = @"c:\projects\projectA\src\";
                string config = "dependencies.config";

                command.FileSystem.FileExists(Arg.Any<string>()).Returns(false);

                command.FileSystem.ReadAllTextFromFile(Arg.Any<string>()).Returns(_json);

                var options = fixture.Build<GetDependenciesOptions>()
                    .With(x => x.ConfigFilePath, executingDir)
                    .Without(x => x.BuildConfigId)
                    .Create();

                Exception exception = Assert.Throws<Exception>(() => command.LoadConfigFile(options, "dependencies.config"));

                Assert.True(exception.Message.StartsWith("Config file not found"));
            }
        }

        public class TestResolveDependencyCommand : ResolveDependencyCommand
        {
            public ITeamCityClient Client { get; private set; }
            public IFileDownloader Downloader { get; private set; }
            public IFileSystem FileSystem { get; private set; }

            public TestResolveDependencyCommand(ITeamCityClient client, IFileDownloader downloader, IFileSystem fileSystem) 
                : base(client, downloader, fileSystem)
            {
                Client = client;
                Downloader = downloader;
                FileSystem = fileSystem;

                FileSystem.GetWorkingDirectory().Returns(@"c:\projects\projectA");
                FileSystem.FileExists(@"c:\projects\projectA\dependencies.config").Returns(true);
                FileSystem.GetFullPath(@"src\assemblies").Returns(@"c:\projects\projectA\src\assemblies");
            }
        }
    }
}