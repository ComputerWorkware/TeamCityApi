using System.Threading.Tasks;
using NSubstitute;
using Ploeh.AutoFixture.Xunit;
using TeamCityApi;
using TeamCityApi.Domain;
using TeamCityConsole.Commands;
using TeamCityConsole.Tests.Helpers;
using TeamCityConsole.Utils;
using Xunit.Extensions;

namespace TeamCityConsole.Tests.Commands
{
    public class UpdateCommandTests
    {
         public class Execute
         {
             [Theory]
             [AutoNSubstituteData]
             internal void Should_not_update_when_version_has_not_changed(
                 Build build,
                 TestUpdateCommand command)
             {
                 command.Client.Builds.LastSuccessfulBuildFromConfig(Arg.Any<string>()).Returns(Task.FromResult(build));
                 command.AssemblyMetada.FileVersion.Returns(build.Number);

                 command.Execute(null).Wait();
             }

             [Theory]
             [AutoNSubstituteData]
             internal void Should_delete_old_exe(
                 Build build,
                 string newFileVersion,
                 TestUpdateCommand command)
             {
                 const string exePath = @"Z:\blah\TeamCityConsole.exe";
                 const string oldExePath = exePath+".old";

                 command.Client.Builds.LastSuccessfulBuildFromConfig(Arg.Any<string>()).Returns(Task.FromResult(build));
                 command.AssemblyMetada.FileVersion.Returns(newFileVersion);
                 command.AssemblyMetada.Location.Returns(exePath);
                 command.FileSystem.FileExists(oldExePath).Returns(true);

                 command.Execute(null).Wait();

                 command.FileSystem.Received().DeleteFile(oldExePath);
             }

             [Theory]
             [AutoNSubstituteData]
             internal void Should_move_exe_to_old_file(
                 Build build,
                 string newFileVersion,
                 TestUpdateCommand command)
             {
                 const string exePath = @"Z:\blah\TeamCityConsole.exe";
                 const string oldExePath = exePath + ".old";

                 command.Client.Builds.LastSuccessfulBuildFromConfig(Arg.Any<string>()).Returns(Task.FromResult(build));
                 command.AssemblyMetada.FileVersion.Returns(newFileVersion);
                 command.AssemblyMetada.Location.Returns(exePath);

                 command.Execute(null).Wait();

                 command.FileSystem.Received().MoveFile(exePath, oldExePath);
             }

             [Theory]
             [AutoNSubstituteData]
             internal void Should_download_new_exe(
                 Build build,
                 string newFileVersion,
                 TestUpdateCommand command)
             {
                 const string exePath = @"Z:\blah\TeamCityConsole.exe";

                 command.Client.Builds.LastSuccessfulBuildFromConfig(Arg.Any<string>()).Returns(Task.FromResult(build));
                 command.AssemblyMetada.FileVersion.Returns(newFileVersion);
                 command.AssemblyMetada.Location.Returns(exePath);

                 command.Execute(null).Wait();

                 command.Downloader.Received().Download(@"Z:\blah", Arg.Is<File>(file =>
                 
                     file.Name == "TeamCityConsole.exe"
                            && file.ContentHref ==
                            string.Format("repository/download/{0}/{1}:id/TeamCityApi.zip!/TeamCityConsole.exe",
                                command.Settings.SelfUpdateBuildConfigId, build.Id)
                 ));
             }
         }

        internal class TestUpdateCommand : UpdateCommand
        {
            public ITeamCityClient Client { get; private set; }
            public IFileSystem FileSystem { get; private set; }
            public IFileDownloader Downloader { get; private set; }
            public IAssemblyMetada AssemblyMetada { get; private set; }
            public Settings Settings { get; private set; }

            public TestUpdateCommand(ITeamCityClient client, IFileSystem fileSystem, IFileDownloader downloader,IAssemblyMetada assemblyMetada, Settings settings) 
                : base(client, fileSystem, downloader, assemblyMetada, settings)
            {
                Client = client;
                FileSystem = fileSystem;
                Downloader = downloader;
                AssemblyMetada = assemblyMetada;
                Settings = settings;
            }
        }
    }
}