using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NLog;
using TeamCityApi;
using TeamCityApi.Domain;
using TeamCityConsole.Options;
using File = TeamCityApi.Domain.File;

namespace TeamCityConsole.Commands
{
    class DownloadArtifactCommand : ICommand
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public async Task Execute(object options)
        {
            var artifactOptions = options as GetArtifactOptions;

            var client = new TeamCityClient(AppSettings.Default.teamcityuri, AppSettings.Default.username, AppSettings.Default.password);

            Build build = await client.Builds.LastSuccessfulBuildFromConfig(artifactOptions.ConfigTypeId);

            List<File> files = await build.ArtifactsReference.GetFiles();

            await DownloadFiles(artifactOptions.OutputDirectory, files);
        }

        private async Task DownloadFiles(string destPath, IEnumerable<File> files)
        {
            foreach (File file in files)
            {
                if (file.HasContent)
                {
                    await Download(destPath, file);
                }
                else
                {
                    List<File> children = await file.GetChildren();
                    await DownloadFiles(Path.Combine(destPath, file.Name), children);
                }
            }
        }

        private async Task Download(string destPath, File file)
        {
            var destFileName = BuildFullName(destPath, file);

            Log.Debug("Downloading: {0}", destFileName);

            EnsureDirectoryExists(destFileName);

            var http = new HttpClientWrapper(AppSettings.Default.teamcityuri, AppSettings.Default.username,AppSettings.Default.password);
            using (Stream stream = await http.GetStream(file.ContentHref))
            {
                using (var fileStream = new FileStream(destFileName, FileMode.Create))
                {
                    await stream.CopyToAsync(fileStream);
                }
            }
        }

        private static string BuildFullName(string destPath, File file)
        {
            //split the unix like name that comes from TC
            string[] pathParts = file.Name.Split('/');

            //join them back using the proper separator for the environment
            string properPath = string.Join(Path.DirectorySeparatorChar.ToString(), pathParts);

            return Path.Combine(destPath, properPath);
        }

        private static void EnsureDirectoryExists(string filePath)
        {
            string directoryName = Path.GetDirectoryName(filePath);

            if (directoryName == null)
            {
                return;
            }

            if (Directory.Exists(directoryName))
            {
                return;
            }

            Directory.CreateDirectory(directoryName);
        }
    }
}