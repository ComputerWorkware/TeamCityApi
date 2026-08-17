using System;
using System.Collections.Generic;
using System.IO;
using TeamCityConsole.Commands;

namespace TeamCityConsole.Utils
{
    /// <summary>
    /// When a .nupkg is downloaded into src/assemblies, removes the matching
    /// folder under src/packages when the downloaded package contents differ.
    /// </summary>
    internal static class AssembliesNupkgPackageCleaner
    {
        private static readonly string AssembliesSegment =
            "src" + Path.DirectorySeparatorChar + "assemblies";

        public static bool IsAssembliesDestination(string destPath)
        {
            if (string.IsNullOrWhiteSpace(destPath))
            {
                return false;
            }

            var normalized = destPath.Replace('/', Path.DirectorySeparatorChar)
                .TrimEnd(Path.DirectorySeparatorChar);

            return normalized.IndexOf(AssembliesSegment, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public static string GetPackageFolderName(string nupkgFileName)
        {
            if (string.IsNullOrWhiteSpace(nupkgFileName))
            {
                return null;
            }

            var fileName = Path.GetFileName(nupkgFileName);
            if (!fileName.EndsWith(".nupkg", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            // Use version for NuGet sub folder.
            var filePath = Path.GetFileNameWithoutExtension(fileName);
            var filePaths = filePath.Split('.');
            if (filePaths.Length > 2)
            {
                var packageName = filePaths[0];
                var packageVersion = string.Join(".", filePaths, 1, filePaths.Length - 1);
                filePath = $"{packageName}\\{packageVersion}";
            }
            return filePath;
        }

        public static void RemoveCachedPackages(
            IFileSystem fileSystem,
            string rootDirectory,
            IEnumerable<PathFilePair> downloadedFiles,
            Action<string> log = null)
        {
            if (fileSystem == null || string.IsNullOrWhiteSpace(rootDirectory) || downloadedFiles == null)
            {
                return;
            }

            var packagesDir = Path.Combine(rootDirectory, "src", "packages");
            if (!fileSystem.DirectoryExists(packagesDir))
            {
                return;
            }

            foreach (var pair in downloadedFiles)
            {
                if (pair?.File == null)
                {
                    continue;
                }

                if (!IsAssembliesDestination(pair.Path))
                {
                    continue;
                }

                var packageFolderName = GetPackageFolderName(pair.File.Name);
                if (packageFolderName == null)
                {
                    continue;
                }

                var packageCachePath = Path.Combine(packagesDir, packageFolderName);
                if (!fileSystem.DirectoryExists(packageCachePath))
                {
                    continue;
                }

                var assemblyFileName = Path.Combine(rootDirectory, pair.Path, pair.File.Name);
                var packageFileName = Path.Combine(packageCachePath, pair.File.Name);

                if (!fileSystem.FileExists(assemblyFileName) || !fileSystem.FileExists(packageFileName))
                {
                    continue;
                }

                if (FilesAreEqual(fileSystem, assemblyFileName, packageFileName))
                {
                    continue;
                }

                log?.Invoke(string.Format("Removing cached NuGet package to force restore: {0}", packageCachePath));
                fileSystem.DeleteDirectory(packageCachePath, true);
            }
        }

        private static bool FilesAreEqual(IFileSystem fileSystem, string firstPath, string secondPath)
        {
            using (var first = fileSystem.OpenFile(firstPath, FileMode.Open))
            using (var second = fileSystem.OpenFile(secondPath, FileMode.Open))
            {
                var firstBuffer = new byte[81920];
                var secondBuffer = new byte[81920];

                while (true)
                {
                    var firstRead = first.Read(firstBuffer, 0, firstBuffer.Length);
                    var secondRead = second.Read(secondBuffer, 0, secondBuffer.Length);

                    if (firstRead != secondRead)
                    {
                        return false;
                    }

                    if (firstRead == 0)
                    {
                        return true;
                    }

                    for (var index = 0; index < firstRead; index++)
                    {
                        if (firstBuffer[index] != secondBuffer[index])
                        {
                            return false;
                        }
                    }
                }
            }
        }
    }
}
