using System;
using NSubstitute;
using Ploeh.AutoFixture.Xunit;
using TeamCityApi;
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
            internal void TheoryMethodName(ITeamCityClient client, IFileDownloader downloader, IFileSystem fileSystem)
            {
                var command = new ResolveDependencyCommand(client, downloader, fileSystem);

                var options = new GetDependenciesOptions();

                command.Execute(options).Wait();
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