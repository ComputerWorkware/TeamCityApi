using System.Threading.Tasks;
using NSubstitute;
using TeamCityApi;
using TeamCityApi.UseCases;
using TeamCityConsole.Commands;
using TeamCityConsole.Options;
using TeamCityConsole.Tests.Helpers;
using TeamCityConsole.Utils;
using Xunit.Extensions;

namespace TeamCityConsole.Tests.Commands
{
    public class ProjectInventoryCommandTests
    {
        public class Execute
        {
            [Theory]
            [AutoNSubstituteData]
            public void should_save_generated_markdown(IFileSystem fileSystem, ITeamCityClient client, ProjectInventoryOptions options)
            {
                options.OutputFile = @"reports\inventory.md";
                var useCase = Substitute.For<ProjectInventoryUseCase>(client);

                fileSystem.GetFullPath(options.OutputFile).Returns(@"C:\work\reports\inventory.md");
                useCase.Execute(options.BuildId).Returns(Task.FromResult("| markdown |"));

                var sut = new ProjectInventoryCommand(useCase, fileSystem);

                sut.Execute(options).Wait();

                fileSystem.Received().EnsureDirectoryExists(@"C:\work\reports\inventory.md");
                fileSystem.Received().WriteAllTextToFile(@"C:\work\reports\inventory.md", "| markdown |");
            }

            [Theory]
            [AutoNSubstituteData]
            public void should_use_default_output_file_when_none_is_provided(IFileSystem fileSystem, ITeamCityClient client, long buildId)
            {
                var useCase = Substitute.For<ProjectInventoryUseCase>(client);
                var options = new ProjectInventoryOptions
                {
                    BuildId = buildId,
                    OutputFile = null
                };

                fileSystem.GetFullPath("project-inventory.md").Returns(@"C:\work\project-inventory.md");
                useCase.Execute(buildId).Returns(Task.FromResult("| markdown |"));

                var sut = new ProjectInventoryCommand(useCase, fileSystem);

                sut.Execute(options).Wait();

                fileSystem.Received().EnsureDirectoryExists(@"C:\work\project-inventory.md");
                fileSystem.Received().WriteAllTextToFile(@"C:\work\project-inventory.md", "| markdown |");
            }
        }
    }
}
