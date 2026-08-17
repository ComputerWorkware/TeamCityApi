using System;
using System.Collections.Generic;
using System.IO;
using NSubstitute;
using TeamCityConsole.Commands;
using TeamCityConsole.Utils;
using Xunit;
using File = TeamCityApi.Domain.File;

namespace TeamCityConsole.Tests.Utils
{
    public class AssembliesNupkgPackageCleanerTests
    {
        [Fact]
        public void IsAssembliesDestination_returns_true_for_src_assemblies_paths()
        {
            Assert.True(AssembliesNupkgPackageCleaner.IsAssembliesDestination(@".\src\assemblies"));
            Assert.True(AssembliesNupkgPackageCleaner.IsAssembliesDestination("src/assemblies"));
            Assert.True(AssembliesNupkgPackageCleaner.IsAssembliesDestination(@"c:\projects\app\src\assemblies"));
        }

        [Fact]
        public void IsAssembliesDestination_returns_false_for_non_assemblies_paths()
        {
            Assert.False(AssembliesNupkgPackageCleaner.IsAssembliesDestination(@".\src\bin"));
            Assert.False(AssembliesNupkgPackageCleaner.IsAssembliesDestination(@"assemblies"));
            Assert.False(AssembliesNupkgPackageCleaner.IsAssembliesDestination(null));
            Assert.False(AssembliesNupkgPackageCleaner.IsAssembliesDestination(""));
        }

        [Fact]
        public void GetPackageFolderName_strips_nupkg_extension()
        {
            Assert.Equal("GSABO\\1.2.3", AssembliesNupkgPackageCleaner.GetPackageFolderName(@"GSABO.1.2.3.nupkg"));
            Assert.Equal("Cwi.Core\\1.2.3", AssembliesNupkgPackageCleaner.GetPackageFolderName("Cwi.Core.1.2.3.nupkg"));
            Assert.Equal("Cwi.Core\\1.2.3", AssembliesNupkgPackageCleaner.GetPackageFolderName(@"path\Cwi.Core.1.2.3.nupkg"));
        }

        [Fact]
        public void GetPackageFolderName_returns_null_for_non_nupkg()
        {
            Assert.Null(AssembliesNupkgPackageCleaner.GetPackageFolderName("Cwi.Core.dll"));
            Assert.Null(AssembliesNupkgPackageCleaner.GetPackageFolderName(null));
        }

        [Fact]
        public void RemoveCachedPackages_deletes_matching_package_folder_when_contents_differ()
        {
            var fileSystem = Substitute.For<IFileSystem>();
            var root = @"c:\projects\app";
            var packagesDir = Path.Combine(root, "src", "packages");
            var packageCachePath = Path.Combine(packagesDir, "Cwi.Core\\1.2.3");
            var assemblyPath = Path.Combine(root, @".\src\assemblies", "Cwi.Core.1.2.3.nupkg");
            var packagePath = Path.Combine(packageCachePath, "Cwi.Core.1.2.3.nupkg");

            fileSystem.DirectoryExists(packagesDir).Returns(true);
            fileSystem.DirectoryExists(packageCachePath).Returns(true);
            fileSystem.FileExists(assemblyPath).Returns(true);
            fileSystem.FileExists(packagePath).Returns(true);
            fileSystem.OpenFile(assemblyPath, FileMode.Open).Returns(new MemoryStream(new byte[] { 1 }));
            fileSystem.OpenFile(packagePath, FileMode.Open).Returns(new MemoryStream(new byte[] { 2 }));

            var downloadedFiles = new List<PathFilePair>
            {
                new PathFilePair
                {
                    Path = @".\src\assemblies",
                    File = new File
                    {
                        Name = "Cwi.Core.1.2.3.nupkg"
                    }
                }
            };

            AssembliesNupkgPackageCleaner.RemoveCachedPackages(fileSystem, root, downloadedFiles);

            fileSystem.Received(1).DeleteDirectory(packageCachePath, true);
        }

        [Fact]
        public void RemoveCachedPackages_keeps_matching_package_folder_when_contents_are_same()
        {
            var fileSystem = Substitute.For<IFileSystem>();
            var root = @"c:\projects\app";
            var packagesDir = Path.Combine(root, "src", "packages");
            var packageCachePath = Path.Combine(packagesDir, "Cwi.Core.1.2.3");
            var assemblyPath = Path.Combine(root, @".\src\assemblies", "Cwi.Core.1.2.3.nupkg");
            var packagePath = Path.Combine(packageCachePath, "Cwi.Core.1.2.3.nupkg");

            fileSystem.DirectoryExists(packagesDir).Returns(true);
            fileSystem.DirectoryExists(packageCachePath).Returns(true);
            fileSystem.FileExists(assemblyPath).Returns(true);
            fileSystem.FileExists(packagePath).Returns(true);
            fileSystem.OpenFile(assemblyPath, FileMode.Open).Returns(new MemoryStream(new byte[] { 1, 2, 3 }));
            fileSystem.OpenFile(packagePath, FileMode.Open).Returns(new MemoryStream(new byte[] { 1, 2, 3 }));

            var downloadedFiles = new List<PathFilePair>
            {
                new PathFilePair
                {
                    Path = @".\src\assemblies",
                    File = new File
                    {
                        Name = "Cwi.Core.1.2.3.nupkg"
                    }
                }
            };

            AssembliesNupkgPackageCleaner.RemoveCachedPackages(fileSystem, root, downloadedFiles);

            fileSystem.DidNotReceive().DeleteDirectory(Arg.Any<string>(), Arg.Any<bool>());
        }

        [Fact]
        public void RemoveCachedPackages_ignores_non_nupkg_and_non_assemblies_downloads()
        {
            var fileSystem = Substitute.For<IFileSystem>();
            var root = @"c:\projects\app";
            var packagesDir = Path.Combine(root, "src", "packages");

            fileSystem.DirectoryExists(packagesDir).Returns(true);

            var downloadedFiles = new List<PathFilePair>
            {
                new PathFilePair
                {
                    Path = @".\src\assemblies",
                    File = new File { Name = "Cwi.Core.dll" }
                },
                new PathFilePair
                {
                    Path = @".\src\bin",
                    File = new File { Name = "Other.1.0.0.nupkg" }
                }
            };

            AssembliesNupkgPackageCleaner.RemoveCachedPackages(fileSystem, root, downloadedFiles);

            fileSystem.DidNotReceive().DeleteDirectory(Arg.Any<string>(), Arg.Any<bool>());
        }

        [Fact]
        public void RemoveCachedPackages_skips_when_package_folder_is_missing()
        {
            var fileSystem = Substitute.For<IFileSystem>();
            var root = @"c:\projects\app";
            var packagesDir = Path.Combine(root, "src", "packages");
            var packageCachePath = Path.Combine(packagesDir, "Cwi.Core.1.2.3");

            fileSystem.DirectoryExists(packagesDir).Returns(true);
            fileSystem.DirectoryExists(packageCachePath).Returns(false);

            var downloadedFiles = new List<PathFilePair>
            {
                new PathFilePair
                {
                    Path = @".\src\assemblies",
                    File = new File { Name = "Cwi.Core.1.2.3.nupkg" }
                }
            };

            AssembliesNupkgPackageCleaner.RemoveCachedPackages(fileSystem, root, downloadedFiles);

            fileSystem.DidNotReceive().DeleteDirectory(Arg.Any<string>(), Arg.Any<bool>());
        }

        [Fact]
        public void RemoveCachedPackages_skips_when_package_files_are_missing()
        {
            var fileSystem = Substitute.For<IFileSystem>();
            var root = @"c:\projects\app";
            var packagesDir = Path.Combine(root, "src", "packages");
            var packageCachePath = Path.Combine(packagesDir, "Cwi.Core.1.2.3");
            fileSystem.DirectoryExists(packagesDir).Returns(true);
            fileSystem.DirectoryExists(packageCachePath).Returns(true);

            var downloadedFiles = new List<PathFilePair>
            {
                new PathFilePair
                {
                    Path = @".\src\assemblies",
                    File = new File
                    {
                        Name = "Cwi.Core.1.2.3.nupkg"
                    }
                }
            };

            AssembliesNupkgPackageCleaner.RemoveCachedPackages(fileSystem, root, downloadedFiles);

            fileSystem.DidNotReceive().DeleteDirectory(Arg.Any<string>(), Arg.Any<bool>());
        }
    }
}
