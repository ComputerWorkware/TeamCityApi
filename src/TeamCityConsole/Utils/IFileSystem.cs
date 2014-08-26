using System;
using System.IO;
using System.Threading.Tasks;

namespace TeamCityConsole.Utils
{
    public interface IFileSystem
    {
        char DirectorySeparator { get; }
        string CombinePath(params string[] paths);
        bool DirectoryExists(string path);
        void CreateDirectory(string path);
        void DeleteDirectory(string path, bool recursive = false);
        bool FileExists(string path);
        string GetDirectoryName(string path);
        void EnsureDirectoryExists(string filePath);
        string ReadAllTextFromFile(string path);
        void WriteAllTextToFile(string path, string contents);
        Stream OpenFile(string path, FileMode fileMode);
        Stream CreateFile(string path);
        string CreateTempFile();
        Task CreateFileFromStreamAsync(string path, Stream stream);
        void MoveFile(string sourceFileName, string destFileName);
        void CopyFile(string sourceFileName, string destFileName, bool overwrite = false);
        void DeleteFile(string fileName);
        string GetFullPath(string path);
        string GetApplicationBaseDirectory();
        string GetWorkingDirectory();
        void ExtractToDirectory(string sourceArchiveFileName, string destDirectoryName);
    }

    class FileSystem : IFileSystem
    {
        public char DirectorySeparator
        {
            get { return Path.DirectorySeparatorChar; }
        }

        public string CombinePath(params string[] paths)
        {
            return Path.Combine(paths);
        }

        public bool DirectoryExists(string path)
        {
            return Directory.Exists(path);
        }

        public void CreateDirectory(string path)
        {
            Directory.CreateDirectory(path);
        }

        public void DeleteDirectory(string path, bool recursive = false)
        {
            Directory.Delete(path, recursive);
        }

        public bool FileExists(string path)
        {
            return File.Exists(path);
        }

        public string GetDirectoryName(string path)
        {
            return Path.GetDirectoryName(path);
        }

        public void EnsureDirectoryExists(string filePath)
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

        public string ReadAllTextFromFile(string path)
        {
            return File.ReadAllText(path);
        }

        public void WriteAllTextToFile(string path, string contents)
        {
            File.WriteAllText(path, contents);
        }

        public Stream OpenFile(string path, FileMode fileMode)
        {
            return File.Open(path, fileMode);
        }

        public Stream CreateFile(string path)
        {
            return File.Create(path);
        }

        public string CreateTempFile()
        {
            return Path.GetTempFileName();
        }

        public async Task CreateFileFromStreamAsync(string path, Stream stream)
        {
            using (var fileStream = new FileStream(path, FileMode.Create))
            {
                await stream.CopyToAsync(fileStream);
            }
        }

        public void MoveFile(string sourceFileName, string destFileName)
        {
            File.Move(sourceFileName, destFileName);
        }

        public void CopyFile(string sourceFileName, string destFileName, bool overwrite = false)
        {
            File.Copy(sourceFileName, destFileName, true);
        }

        public void DeleteFile(string fileName)
        {
            File.Delete(fileName);
        }

        public string GetFullPath(string path)
        {
            return Path.GetFullPath(path);
        }

        public string GetApplicationBaseDirectory()
        {
            return AppDomain.CurrentDomain.BaseDirectory;
        }

        public string GetWorkingDirectory()
        {
            return Path.GetFullPath(".");
        }

        public void ExtractToDirectory(string sourceArchiveFileName, string destDirectoryName)
        {
            System.IO.Compression.ZipFile.ExtractToDirectory(sourceArchiveFileName, destDirectoryName);
        }
    }
}