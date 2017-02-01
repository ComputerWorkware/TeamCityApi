using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NLog;
using TeamCityApi;
using File = TeamCityApi.Domain.File;

namespace TeamCityConsole.Utils
{
    public interface IFileDownloader
    {
        Task Download(string destPath, File file);
    }

    public class FileDownloader : IFileDownloader
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private readonly IHttpClientWrapper _http;

        private readonly IFileSystem _fileSystem;

        public FileDownloader(IHttpClientWrapper http, IFileSystem fileSystem)
        {
            _http = http;
            _fileSystem = fileSystem;
        }

        public async Task Download(string destPath, File file)
        {
            bool unzip = false;

            if (file.Name.EndsWith("!**"))
            {
                file.Name = file.Name.Replace("!**", string.Empty);
                unzip = true;
            }

            if (file.Name=="**")
            {
                file.ContentHref = file.ChildrenHref.Replace("!/**", string.Empty);
                string[] parts = file.ContentHref.Split('/');
                file.Name = parts[parts.Length-1];
                unzip = true;
            }

            var destFileName = BuildFullName(destPath, file);

            EnsureDirectoryExists(destFileName);

            using (Stream stream = await _http.GetStream(file.ContentHref))
            {
                EnsureDirectoryExists(destFileName);
                await _fileSystem.CreateFileFromStreamAsync(destFileName, stream);
            }

            if (unzip)
            {
                Log.Debug("Unzipping "+destFileName);

                var tempFileName = _fileSystem.CreateTempFile();
                _fileSystem.CopyFile(destFileName, tempFileName, true);
                //if (_fileSystem.DirectoryExists(destPath))
                //{
                //    _fileSystem.DeleteDirectory(destPath, true);
                //}
                _fileSystem.ExtractToDirectory(tempFileName,destPath);
                _fileSystem.DeleteFile(tempFileName);
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

        private void EnsureDirectoryExists(string filePath)
        {
            string directoryName = Path.GetDirectoryName(filePath);

            if (directoryName == null)
            {
                return;
            }

            if (_fileSystem.DirectoryExists(directoryName))
            {
                return;
            }

            _fileSystem.CreateDirectory(directoryName);

            if (!_fileSystem.DirectoryExists(directoryName))
            {
                Thread.Sleep(500); // Wait as OS may now have committed the change.
            }
        }
    }
}