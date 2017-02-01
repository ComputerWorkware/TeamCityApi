using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NLog;
using TeamCityApi;
using TeamCityApi.Domain;
using TeamCityApi.Helpers.Git;
using TeamCityApi.Model;
using TeamCityApi.UseCases;
using TeamCityConsole.Options;
using TeamCityConsole.Utils;
using LibGit2Sharp;
using File = TeamCityApi.Domain.File;

namespace TeamCityConsole.Commands
{
    public class GenerateEscrowCommand : ICommand
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private readonly ITeamCityClient _client;
        private readonly GenerateEscrowUseCase _generateEscrowUseCase;
        private readonly ICommand _downloadCommand;
        private readonly IFileSystem _fileSystem;
        private readonly IFileDownloader _downloader;
        private IDownloadDataFlow _downloadDataFlow;

        private readonly Dictionary<string, BuildInfo> _builds = new Dictionary<string, BuildInfo>();

        public GenerateEscrowCommand(ITeamCityClient client, GenerateEscrowUseCase generateEscrowUseCase,
            ICommand downloadCommand, IFileSystem fileSystem, IFileDownloader downloader)
        {
            _client = client;
            _generateEscrowUseCase = generateEscrowUseCase;
            _downloadCommand = downloadCommand;
            _fileSystem = fileSystem;
            _downloader = downloader;
        }

        public async Task Execute(object options)
        {
            var generateEscrowOptions = options as GenerateEscrowOptions;

            string outputPath = generateEscrowOptions.OutputDirectory;
            if (!outputPath.EndsWith(Path.DirectorySeparatorChar.ToString()))
                outputPath += Path.DirectorySeparatorChar.ToString();

            DirectoryInfo outputFolder = new DirectoryInfo(generateEscrowOptions.OutputDirectory);
            if (!outputFolder.Exists)
            {
                outputFolder.Create();
            }
            else
            {
                if (outputFolder.EnumerateFiles("*.*", SearchOption.AllDirectories).Any())
                {
                    Log.Error(
                        $"Output Folder: {outputFolder.FullName} is not empty. Escrow can be created in a empty folder only.");
                    return;
                }
            }

            _fileSystem.EnsureDirectoryExists(outputPath);
            Log.Info("Generating Escrow for: Build: {0} into folder: {1}", generateEscrowOptions.BuildId,
                generateEscrowOptions.OutputDirectory);

            var escrowElements = await GenerateEscrowManifest(generateEscrowOptions, outputPath);

            if (generateEscrowOptions.GenerateManifestOnly)
            {
                Log.Info("================ Generate Escrow: done ================");
                return;
            }

            // Download Nuget
            Log.Info("Downloading Nuget.exe latest from: https://dist.nuget.org/win-x86-commandline/latest/nuget.exe");
            string nugetUrl = "https://dist.nuget.org/win-x86-commandline/latest/nuget.exe";
            await DownloadNuget(nugetUrl, outputPath);

            var credentialList = new List<Credential>
            {
                new Credential
                {
                    HostName = "*",
                    UserName = generateEscrowOptions.User,
                    Password = generateEscrowOptions.Password
                }
            };

            foreach (EscrowElement element in escrowElements)
            {

                string elementPath = _fileSystem.CombinePath(outputPath, element.BuildTypeId);

                CloneRespository(element, elementPath, credentialList);

                await FetchBuildArtifacts(element, elementPath);

                await FetchArtifactDependencies(elementPath, element);

                FetchNuGetPackages(outputPath, elementPath, element);
            }

            Log.Info("================ Generate Escrow: done ================");

        }

        private async Task<bool> DownloadNuget(string nugetUrl, string outputPath)
        {
            _downloadDataFlow = new DownloadDataFlow(_downloader);

            await _downloader.Download(outputPath, new File {ContentHref = nugetUrl, Name = "nuget.exe"});

            _downloadDataFlow.Complete();

            await _downloadDataFlow.Completion;

            return true;
        }

        private void FetchNuGetPackages(string outputPath, string elementPath, EscrowElement element)
        {
            var directoryInfo = new DirectoryInfo(elementPath);
            foreach (var solutionFile in directoryInfo.EnumerateFiles("*.sln", SearchOption.AllDirectories))
            {
                System.Diagnostics.Process process = new System.Diagnostics.Process();
                System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
                startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                startInfo.FileName = Path.Combine(outputPath, "nuget.exe");

                startInfo.Arguments = $"restore \"{solutionFile.FullName}\"";
                startInfo.RedirectStandardOutput = true;
                startInfo.UseShellExecute = false;
                process.StartInfo = startInfo;
                process.Start();
                var output = process.StandardOutput.ReadToEnd();
                Log.Info(output);
                process.WaitForExit();
            }
        }

        private void PullSubmodules(string outputPath)
        {
            var directoryInfo = new DirectoryInfo(outputPath);
            if (directoryInfo.EnumerateFiles(".gitmodules").Any() == false)
                return;

            var currentDirectory = Directory.GetCurrentDirectory();
            Directory.SetCurrentDirectory(directoryInfo.FullName);
            System.Diagnostics.Process process = new System.Diagnostics.Process();
            System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
            startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            startInfo.FileName = "git.exe";

            startInfo.Arguments = $"submodule update --init";
            startInfo.RedirectStandardOutput = true;
            startInfo.UseShellExecute = false;
            process.StartInfo = startInfo;
            process.Start();
            var output = process.StandardOutput.ReadToEnd();
            Log.Info("Attempting to pull submodules");
            Log.Info(output);
            process.WaitForExit();
            Directory.SetCurrentDirectory(currentDirectory);
        }


        private async Task<List<EscrowElement>> GenerateEscrowManifest(GenerateEscrowOptions generateEscrowOptions,
            string outputPath)
        {
            Settings settings = new Settings();
            settings.Load();

            var client = new TeamCityClient(settings.TeamCityUri, settings.Username, settings.Password);

            List<EscrowElement> escrowElements =
                await _generateEscrowUseCase.BuildEscrowList(generateEscrowOptions.BuildId);

            await
                _generateEscrowUseCase.SaveDocument(escrowElements,
                    Path.Combine(outputPath, $"EscrowManifest_BuildId_{generateEscrowOptions.BuildId}.json"));
            return escrowElements;
        }

        private async Task FetchArtifactDependencies(string elementPath, EscrowElement element)
        {
            _downloadDataFlow = new DownloadDataFlow(_downloader);
            _builds.Clear();
            var currentDirectory = Directory.GetCurrentDirectory();
            try
            {
                if (!Directory.Exists(elementPath))
                    Directory.CreateDirectory(elementPath);

                Directory.SetCurrentDirectory(elementPath);
                await ResolveDependencies(element.BuildTypeId, element);
            }
            finally
            {
                Directory.SetCurrentDirectory(currentDirectory);
            }
        }

        private async Task FetchBuildArtifacts(EscrowElement element, string elementPath)
        {
            Log.Info($"Fetch build Artifacts: for {element.BuildTypeId}");
            var directoryInfo = new DirectoryInfo(elementPath);
            if (directoryInfo.Exists == false)
            {
                _fileSystem.CreateDirectory(elementPath);
            }

            string artifactOutputPath = _fileSystem.CombinePath(elementPath, "build");
            _fileSystem.EnsureDirectoryExists(artifactOutputPath);
            await
                _downloadCommand.Execute(new GetArtifactOptions
                {
                    BuildConfigId = element.BuildTypeId,
                    BuildId = element.Id,
                    Tag = string.Empty,
                    OutputDirectory = artifactOutputPath
                });
        }

        private static void CloneRespository(EscrowElement element, string elementPath, List<Credential> credentialList)
        {
            if (!string.IsNullOrWhiteSpace(element.VersionControlHash))
            {
                var uriBuilder = new UriBuilder(element.VersionControlServer);
                uriBuilder.Path = GetFixedVersionControlPath(element) + ".git";
                var gitRepo = new GitRepositoryHttp(uriBuilder.Uri.ToString(), elementPath, string.Empty,
                    credentialList);
                try
                {
                    gitRepo.Clone(new CloneOptions { RecurseSubmodules = true, BranchName = element.VersionControlBranch });
                }
                catch (Exception ex)
                {
                    Log.Error($"Failure during cloning: Exception: {ex} \nStackTrace: \n{ex.StackTraceEx()}");
                    var checkoutFolder = new DirectoryInfo(elementPath);
                    checkoutFolder.Delete(true);
                    gitRepo.Clone(new CloneOptions { RecurseSubmodules = false, BranchName = element.VersionControlBranch });
                }
                gitRepo.AddBranch($"Escrow-{element.Number}", element.VersionControlHash);
                gitRepo.CheckoutBranch($"Escrow-{element.Number}");
            }
        }

        private static string GetFixedVersionControlPath(EscrowElement element)
        {
            if (element.VersionControlPath.Contains("%system.teamcity.projectName%"))
            {
                string[] projectNameParts = element.ProjectName.Split(new string[] {"::"},StringSplitOptions.None);

                element.VersionControlPath = element.VersionControlPath.Replace("%system.teamcity.projectName%", projectNameParts[projectNameParts.Length-1].Trim());
                return element.VersionControlPath;
            }
            return element.VersionControlPath;
        }

        public async Task<bool> ResolveDependencies(string id, EscrowElement escrowElement)
        {
            await ResolveDependenciesInternal(id, escrowElement);

            _downloadDataFlow.Complete();

            await _downloadDataFlow.Completion;

            return true;
        }

        private async Task ResolveDependenciesInternal(string buildConfigId, EscrowElement escrowElement)
        {
            Log.Info("Resolving dependencies for: {0} : {1}", buildConfigId, escrowElement.Number);

            BuildConfig buildConfig = await _client.BuildConfigs.GetByConfigurationId(buildConfigId);

            var tasks = buildConfig.ArtifactDependencies.Select(ad => ResolveDependency(ad, escrowElement));

            await Task.WhenAll(tasks);
        }

        private async Task ResolveDependency(DependencyDefinition dependency, EscrowElement escrowElement)
        {
            Log.Debug("Trying to fetch dependency: {0}", dependency.SourceBuildConfig.Id);

            if (_builds.ContainsKey(dependency.SourceBuildConfig.Id))
            {
                Log.Info("Dependency already fetched. Skipping: {0}", dependency.SourceBuildConfig.Id);
                return;
            }

            EscrowArtifactDependency escrowArtifact =
                escrowElement.ArtifactDependencies.FirstOrDefault(x => x.BuildTypeId == dependency.SourceBuildConfig.Id);

            if (escrowArtifact == null)
            {
                Log.Info("Cannot find dependency defined in escrow docucment: {0}", dependency.SourceBuildConfig.Id);
                return;
            }

            Build build = await _client.Builds.ById(escrowArtifact.Id);

            lock (_builds)
            {
                _builds.Add(build.BuildTypeId, BuildInfo.FromBuild(build));
            }

            Log.Debug("Downloading artifacts from: {0}-{1}", build.BuildTypeId, build.Number);

            List<ArtifactRule> artifactRules = GetArtifactRules(dependency);

            string basePath = _fileSystem.GetWorkingDirectory();

            if (string.IsNullOrWhiteSpace(basePath))
            {
                basePath = ".";
            }

            List<PathFilePair> files = new List<PathFilePair>();
            foreach (ArtifactRule artifactRule in artifactRules)
            {
                files.AddRange(await FetchFileListForArtifactRule(artifactRule, build, basePath));
            }

            DownloadFiles(files);
          
            Log.Debug("Done fetching dependency for: {0}", dependency.SourceBuildConfig.Id);
        }

        private async Task<List<PathFilePair>> FetchFileListForArtifactRule(ArtifactRule artifactRule, Build build,string basePath)
        {
            var files = new List<PathFilePair>();

            if (artifactRule.Source.EndsWith("!**") || artifactRule.Source.EndsWith("!/**"))
            {
                artifactRule.Source.Replace("!/**", "!**");
                files.Add(new PathFilePair
                {
                    File = artifactRule.CreateTeamCityFileReference(build.Href + "/artifacts/content/"),
                    Path = Path.Combine(basePath, artifactRule.Dest)
                });
            }
            else
            {
                if (artifactRule.Source.Contains("*") || artifactRule.Source.Contains("?"))
                {
                    List<TeamCityApi.Domain.File> artifactFiles = new List<File>();
                    string[] paths = artifactRule.Source.Split('/');
                    string fileName = paths[paths.Length - 1];
                    string pathPart = artifactRule.Source.Replace(fileName, String.Empty);

                    Console.WriteLine("Filename: {0}",fileName);
                    Console.WriteLine("Path: {0} -> Source: {1}",pathPart,artifactRule.Source);
                    artifactFiles.AddRange(await GetAllArtifactFiles(build, pathPart, fileName));

                    foreach (var artifactFile in artifactFiles)
                    {
                        artifactRule.Source = (string.IsNullOrWhiteSpace(pathPart)==false ? pathPart + "/" : "") + artifactFile.Name;

                        files.Add(new PathFilePair
                        {
                            File = artifactRule.CreateTeamCityFileReference(build.Href + "/artifacts/content/"),
                            Path = Path.Combine(basePath, artifactRule.Dest)
                        });
                    }

                }
                else
                {
                    files.Add(new PathFilePair
                    {
                        File = artifactRule.CreateTeamCityFileReference(build.Href + "/artifacts/content/"),
                        Path = Path.Combine(basePath, artifactRule.Dest)
                    });
                }
            }

            return files;
        }

        private async Task<IEnumerable<File>> GetAllArtifactFiles(Build build, string folder, string locatorPattern)
        {
            List<File> artifactsFiles;
            if (string.IsNullOrWhiteSpace(folder))
            {
                artifactsFiles = await _client.Builds.GetFiles(build.Id);
            }
            else
            {
                artifactsFiles = await _client.Builds.GetFiles(build.Id, folder, locatorPattern);
            }

            return artifactsFiles;

        }

        private static List<ArtifactRule> GetArtifactRules(DependencyDefinition dependency)
        {
            Property artifactRulesProperty =
                dependency.Properties.Property.FirstOrDefault(
                    x => x.Name.Equals("pathRules", StringComparison.InvariantCultureIgnoreCase));

            if (artifactRulesProperty == null || string.IsNullOrWhiteSpace(artifactRulesProperty.Value))
            {
                throw new Exception(string.Format("Missing or invalid Artifact dependency. ProjectId: {0}", dependency.SourceBuildConfig.ProjectId));
            }

            List<ArtifactRule> artifactRules = ArtifactRule.Parse(artifactRulesProperty.Value);

            return artifactRules;
        }

        private void DownloadFiles(IEnumerable<PathFilePair> files)
        {
            foreach (var pair in files)
            {
                _downloadDataFlow.Download(pair);
            }
        }


    }
}