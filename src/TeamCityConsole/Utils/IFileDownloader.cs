using System.IO;
using System.Threading.Tasks;
using TeamCityApi;
using File = TeamCityApi.Domain.File;

namespace TeamCityConsole.Utils
{
    public interface IFileDownloader
    {
        Task Download(string destPath, File file);
    }

    class FileDownloader : IFileDownloader
    {
        private readonly IHttpClientWrapper _http;

        public FileDownloader(IHttpClientWrapper http)
        {
            _http = http;
        }

        public async Task Download(string destPath, File file)
        {
            bool unzip = false;

            if (file.Name.EndsWith("!**"))
            {
                file.Name = file.Name.Replace("!**", string.Empty);
                unzip = true;
            }

            var destFileName = BuildFullName(destPath, file);

            //Log.Debug("Downloading: {0}", destFileName);

            EnsureDirectoryExists(destFileName);

            using (Stream stream = await _http.GetStream(file.ContentHref))
            {
                using (var fileStream = new FileStream(destFileName, FileMode.Create))
                {
                    await stream.CopyToAsync(fileStream);
                }
            }

            if (unzip)
            {
                System.IO.Compression.ZipFile.ExtractToDirectory(destFileName,destPath);
                System.IO.File.Delete(destFileName);
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