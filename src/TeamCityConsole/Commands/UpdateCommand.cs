using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using TeamCityApi;
using TeamCityApi.Domain;
using TeamCityConsole.Utils;
using File = TeamCityApi.Domain.File;

namespace TeamCityConsole.Commands
{
    class UpdateCommand : ICommand
    {
        private readonly ITeamCityClient _client;

        private readonly IFileSystem _fileSystem;

        private readonly IFileDownloader _downloader;

        private readonly IAssemblyMetada _assemblyMetada;

        private readonly Settings _settings;

        public UpdateCommand(ITeamCityClient client, IFileSystem fileSystem, IFileDownloader downloader, IAssemblyMetada assemblyMetada, Settings settings)
        {
            _client = client;
            _fileSystem = fileSystem;
            _downloader = downloader;
            _assemblyMetada = assemblyMetada;
            _settings = settings;
        }

        public async Task Execute(object options)
        {
            Build build = await _client.Builds.LastSuccessfulBuildFromConfig(_settings.SelfUpdateBuildConfigId);

            if (build.Number == _assemblyMetada.FileVersion)
            {
                return;
            }

            string exePath = _assemblyMetada.Location;


            string renamedPath = exePath + ".old";
            Move(exePath, renamedPath);

            var file = new TeamCityApi.Domain.File()
            {
                Name = "TeamCityConsole.exe",
                ContentHref = string.Format("repository/download/{0}/{1}:id/TeamCityApi.zip!/TeamCityConsole.exe", _settings.SelfUpdateBuildConfigId, build.Id)
            };

            await _downloader.Download(Path.GetDirectoryName(exePath), file);
        }

        protected void Move(string oldPath, string newPath)
        {
            try
            {
                if (_fileSystem.FileExists(newPath))
                {
                    _fileSystem.DeleteFile(newPath);
                }
            }
            catch (FileNotFoundException)
            {

            }

            _fileSystem.MoveFile(oldPath, newPath);
        }
    }
}