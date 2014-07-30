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
        private const string EmptyConfigJson = @"
{
  ""BuildConfigId"": ""MyConfig"",
  ""BuildInfos"": []
}";

        public class PathRules
        {
            [Theory]
            [AutoNSubstituteData]
            internal void Should_download_file_from_dependency_with_rule_destination_as_target_directory(
                TestResolveDependencyCommand command,
                string buildConfigId,
                IFixture fixture)
            {
                var dependencyDefinition = fixture.Build<DependencyDefinition>()
                    .WithPathRules("file.dll => somedir/assemblies")
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

                command.Downloader.Received().Download(".\\somedir\\assemblies", Arg.Any<File>());
            }

            [Theory]
            [AutoNSubstituteData]
            internal void Should_handle_emtpy_destination_directory_on_Path_Rule(
                TestResolveDependencyCommand command,
                string buildConfigId,
                IFixture fixture)
            {
                var dependencyDefinition = fixture.Build<DependencyDefinition>()
                    .WithPathRules("file.dll => ")
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

                command.Downloader.Received().Download(".", Arg.Any<File>());
            }

            [Theory]
            [AutoNSubstituteData]
            internal void Should_download_each_dependency_in_the_path_rules(
                TestResolveDependencyCommand command,
                string buildConfigId,
                IFixture fixture)
            {
                //2 files are defined in the path rules
                var dependencyDefinition = fixture.Build<DependencyDefinition>()
                    .WithPathRules("fileA.dll => path1/assemblies" + Environment.NewLine + "fileB.dll=>path2/assemblies")
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

                //ensures 2 files were downloaded
                command.Downloader.Received(1).Download(".\\path1\\assemblies", Arg.Any<File>());
                command.Downloader.Received(1).Download(".\\path2\\assemblies", Arg.Any<File>());
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

                command.FileSystem.ReadAllTextFromFile(projectDir + config).Returns(EmptyConfigJson);

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
        }

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

                command.Downloader.Received().Download(".\\src\\assemblies", Arg.Any<File>());
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
                command.Downloader.Received().Download(".\\src\\assemblies", Arg.Is<File>(file => file.Name == "fileB.dll"));
                command.Downloader.Received().Download(".\\src\\assemblies", Arg.Is<File>(file => file.Name == "fileC.dll"));
            }
        }

        public class Config
        {
            [Theory]
            [AutoNSubstituteData]
            internal void Should_initialize_DependencyConfig_with_empty_BuildInfos_list_When_config_file_does_not_exist(
                TestResolveDependencyCommand command,
                string buildConfigId, 
                IFixture fixture)
            {
                command.FileSystem.FileExists(@"c:\projects\projectA\dependencies.config").Returns(false);

                BuildConfig buildConfig = fixture.Build<BuildConfig>()
                    .WithId(buildConfigId)
                    .WithNoDependencies()
                    .Create();

                command.Client.BuildConfigs.GetByConfigurationId(buildConfigId).Returns(Task.FromResult(buildConfig));

                DependencyConfig config = null;

                command.FileSystem.When(x => x.WriteAllTextToFile(Arg.Any<string>(), Arg.Any<string>()))
                    .Do(info =>
                    {
                        var content = info.Args()[1] as string;
                        config = Json.Deserialize<DependencyConfig>(content);
                    });

                var options = fixture.Build<GetDependenciesOptions>()
                    .With(x => x.BuildConfigId, buildConfigId)
                    .Without(x => x.ConfigFilePath)
                    .Create();

                command.Execute(options).Wait();

                command.FileSystem.Received().WriteAllTextToFile(Arg.Any<string>(), Arg.Any<string>());

                Assert.Equal(buildConfigId, config.BuildConfigId);
                Assert.Empty(config.BuildInfos);
            }

            [Theory]
            [AutoNSubstituteData]
            internal void Should_initialize_new_config_from_command_line_options_When_config_file_does_not_exist(
                TestResolveDependencyCommand command,
                GetDependenciesOptions options,
                string fileName)
            {
                command.FileSystem.FileExists(Arg.Any<string>()).Returns(false);

                DependencyConfig config = command.LoadConfigFile(options, fileName);

                Assert.Equal(options.BuildConfigId, config.BuildConfigId);
                Assert.Empty(config.BuildInfos);
            }

            [Theory]
            [AutoNSubstituteData]
            internal void Should_throw_exception_if_Force_is_false_AND_Config_file_not_found_When_config_file_does_not_exist(
                TestResolveDependencyCommand command,
                GetDependenciesOptions options,
                string fileName)
            {
                command.FileSystem.FileExists(Arg.Any<string>()).Returns(false);

                //BuildConfigId set to null will cause Force == false
                options.BuildConfigId = null;

                Exception exception = Assert.Throws<Exception>(() => command.LoadConfigFile(options, fileName));

                Assert.True(exception.Message.StartsWith("Config file not found"));
            }

            [Theory]
            [AutoNSubstituteData]
            internal void Load_options_from_file_When_config_file_exists(
                TestResolveDependencyCommand command,
                IFixture fixture)
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
                var options = fixture.Build<GetDependenciesOptions>()
                    .Without(x => x.BuildConfigId)
                    .Without(x => x.ConfigFilePath)
                    .Create();

                command.FileSystem.ReadAllTextFromFile(command.DefaultConfigLocation).Returns(json);

                DependencyConfig config = command.LoadConfigFile(options, "dependencies.config");

                Assert.Equal("MyConfig", config.BuildConfigId);
                Assert.Equal(2, config.BuildInfos.Count);
            }

            [Theory]
            [AutoNSubstituteData]
            public void Should_search_for_config_on_containing_folders(IFixture fixture,
                TestResolveDependencyCommand command)
            {
                string executingDir = @"c:\projects\projectA\src\";

                command.FileSystem.GetWorkingDirectory().Returns(executingDir);
                command.FileSystem.FileExists(Arg.Any<string>()).Returns(false);

                var options = fixture.Build<GetDependenciesOptions>()
                    .Without(x => x.ConfigFilePath)
                    .Without(x => x.BuildConfigId)
                    .Create();

                try
                {
                    command.LoadConfigFile(options, "dependencies.config");
                }
                catch (Exception e)
                {
                    //An exception happens because the config files is not found.
                    //Do nothing, we just want to test that all paths were probed
                }

                command.FileSystem.Received().FileExists(@"c:\projects\projectA\src\dependencies.config");
                command.FileSystem.Received().FileExists(@"c:\projects\projectA\dependencies.config");
                command.FileSystem.Received().FileExists(@"c:\projects\dependencies.config");
                command.FileSystem.Received().FileExists(@"c:\dependencies.config");
            }

            [Theory]
            [AutoNSubstituteData]
            public void Should_throw_exception_when_config_not_found(IFixture fixture,
                TestResolveDependencyCommand command)
            {
                string executingDir = @"c:\projects\projectA\src\";
                string config = "dependencies.config";

                command.FileSystem.FileExists(Arg.Any<string>()).Returns(false);

                command.FileSystem.ReadAllTextFromFile(Arg.Any<string>()).Returns(EmptyConfigJson);

                var options = fixture.Build<GetDependenciesOptions>()
                    .With(x => x.ConfigFilePath, executingDir)
                    .Without(x => x.BuildConfigId)
                    .Create();

                Exception exception = Assert.Throws<Exception>(() => command.LoadConfigFile(options, "dependencies.config"));

                Assert.True(exception.Message.StartsWith("Config file not found"));
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

        public class TestResolveDependencyCommand : ResolveDependencyCommand
        {
            public ITeamCityClient Client { get; private set; }
            public IFileDownloader Downloader { get; private set; }
            public IFileSystem FileSystem { get; private set; }
            public string DefaultConfigLocation { get; private set; }

            public TestResolveDependencyCommand(ITeamCityClient client, IFileDownloader downloader, IFileSystem fileSystem) 
                : base(client, downloader, fileSystem)
            {
                Client = client;
                Downloader = downloader;
                FileSystem = fileSystem;

                DefaultConfigLocation = @"c:\projects\projectA\dependencies.config";

                FileSystem.GetWorkingDirectory().Returns(@"c:\projects\projectA");
                FileSystem.FileExists(DefaultConfigLocation).Returns(true);
                FileSystem.GetFullPath(@"src\assemblies").Returns(@"c:\projects\projectA\src\assemblies");
            }
        }
    }
}