using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TeamCityApi.Helpers.Git
{
    public static class GitHelper
    {
        public static string GetCurrentBranchName(string path)
        {
            var gitFolder = GetGitFolder(path);
            if (string.IsNullOrWhiteSpace(gitFolder)) return string.Empty;

            var headFile = GetHeadFile(gitFolder);
            if (string.IsNullOrWhiteSpace(headFile)) return string.Empty;

            var headFileContent = File.ReadAllText(headFile);
            return ExtractBranchNameFromHeadFile(headFileContent);
        }

        private static string GetGitFolder(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path)) return string.Empty;

            var currentPath = new DirectoryInfo(path);
            bool isAGitRepo;
            string gitFolder;
            do
            {
                gitFolder = Path.Combine(currentPath.FullName, ".git");
                isAGitRepo = Directory.Exists(gitFolder);
                if (isAGitRepo) break;

                currentPath = currentPath.Parent;
            } while (currentPath != null && currentPath.Exists);


            return isAGitRepo ? gitFolder : string.Empty;
        }

        private static string GetHeadFile(string gitFolder)
        {
            var headFile = Path.Combine(gitFolder, "HEAD");
            var headFileExists = File.Exists(headFile);

            return headFileExists ? headFile : string.Empty;
        }

        private static string ExtractBranchNameFromHeadFile(string headFileContent)
        {
            return headFileContent.Replace(@"ref: refs/heads/", string.Empty)
                .Replace(Environment.NewLine, string.Empty)
                .Trim();
        }
    }
}
