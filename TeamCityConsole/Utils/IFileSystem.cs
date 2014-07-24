using System.IO;

namespace TeamCityConsole.Utils
{
    public interface IFileSystem
    {
        char DirectorySeparator { get; }
        string CombinePath(params string[] paths);
        bool DirectoryExists(string path);
        void CreateDirectory(string path);
        bool FileExists(string path);
        string GetDirectoryName(string path);
        void EnsureDirectoryExists(string filePath);
        string ReadAllTextFromFile(string path);
        void WriteAllTextToFile(string path, string contents);
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
    }
}