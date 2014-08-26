using NSubstitute;
using Ploeh.AutoFixture.Xunit;
using TeamCityApi.Domain;
using TeamCityConsole.Tests.Helpers;
using TeamCityConsole.Utils;
using Xunit.Extensions;

namespace TeamCityConsole.Tests.Utils
{
    public class FileDownloaderTests
    {
        [Theory]
        [AutoNSubstituteData]
        public void Should_unzip_when_double_star_used([Frozen]IFileSystem fileSystem, FileDownloader downloader)
        {
            var file = new File() { Name = "web.zip!**", ContentHref = ""};

            string tempFile = @"c:\temp\abc.tmp";

            fileSystem.CreateTempFile().Returns(tempFile);

            downloader.Download(@"c:\temp", file).Wait();

            fileSystem.Received().ExtractToDirectory(tempFile, @"c:\temp");
        }

        [Theory]
        [AutoNSubstituteData]
        public void Should_delete_target_directory_when_unziping([Frozen]IFileSystem fileSystem, FileDownloader downloader)
        {
            var file = new File() { Name = "web.zip!**", ContentHref = "" };
            
            var destPath = @"c:\temp";

            fileSystem.DirectoryExists(destPath).Returns(true);

            downloader.Download(destPath, file).Wait();

            fileSystem.Received().DeleteDirectory(destPath, true);
        } 
    }
}