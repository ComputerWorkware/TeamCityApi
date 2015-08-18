using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NLog;
using TeamCityApi;
using TeamCityApi.Domain;
using TeamCityConsole.Options;
using TeamCityConsole.Utils;
using File = TeamCityApi.Domain.File;

namespace TeamCityConsole.Commands
{
    class DownloadArtifactCommand : ICommand
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private readonly IFileSystem _fileSystem;

        public DownloadArtifactCommand() : this(new FileSystem())
        {
            
        }

        public DownloadArtifactCommand(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
        }

        public async Task Execute(object options)
        {
            var artifactOptions = options as GetArtifactOptions;

            Log.Info("Getting artifacts for: {0}, {1}", artifactOptions.BuildConfigId, string.IsNullOrEmpty(artifactOptions.Tag) ? "not by tag" : "by \"" + artifactOptions.Tag + "\" tag");

            Settings settings = new Settings();
            settings.Load();

            var client = new TeamCityClient(settings.TeamCityUri, settings.Username, settings.Password);

            Build build = await client.Builds.LastSuccessfulBuildFromConfig(artifactOptions.BuildConfigId, artifactOptions.Tag);

            Log.Info("Build Number: {0}", build.Number);

            List<File> files = await build.ArtifactsReference.GetFiles();

            await DownloadFiles(artifactOptions.OutputDirectory, files);

            Log.Info("================ Get Artifacts: done ================");
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
                    await DownloadFiles(_fileSystem.CombinePath(destPath, file.Name), children);
                }
            }
        }

        private async Task Download(string destPath, File file)
        {
            var destFileName = BuildFullName(destPath, file);

            Log.Debug("Downloading: {0}", destFileName);

            _fileSystem.EnsureDirectoryExists(destFileName);
            Settings settings = new Settings();
            settings.Load();

            var http = new HttpClientWrapper(settings.TeamCityUri, settings.Username, settings.Password);

            using (Stream stream = await http.GetStream(file.ContentHref))
            {
                using (var fileStream = new FileStream(destFileName, FileMode.Create))
                {
                    await stream.CopyToAsync(fileStream);
                }
            }
        }

        private string BuildFullName(string destPath, File file)
        {
            //split the unix like name that comes from TC
            string[] pathParts = file.Name.Split('/');

            //join them back using the proper separator for the environment
            string properPath = string.Join(Path.DirectorySeparatorChar.ToString(), pathParts);

            return _fileSystem.CombinePath(destPath, properPath);
        }
    }
}