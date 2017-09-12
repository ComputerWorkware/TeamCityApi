using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NLog;
using TeamCityApi;
using TeamCityApi.Domain;
using TeamCityApi.Model;
using TeamCityConsole.Options;
using TeamCityConsole.Utils;
using File = TeamCityApi.Domain.File;
using System.Text.RegularExpressions;
using TeamCityApi.Helpers.Git;

namespace TeamCityConsole.Commands
{
    public class ResolveDependencyCommand : ICommand
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private readonly ITeamCityClient _client;

        private readonly IDownloadDataFlow _downloadDataFlow;

        private readonly IFileSystem _fileSystem;

        private DependencyConfig _dependencyConfig;

        private readonly Dictionary<string, BuildInfo> _builds = new Dictionary<string, BuildInfo>();

        private string _configFullPath;

        private string _majorVersion;

        private string _minorVersion;

        public ResolveDependencyCommand(ITeamCityClient client, IFileSystem fileSystem, IDownloadDataFlow downloadDataFlow)
        {
            _fileSystem = fileSystem;
            _downloadDataFlow = downloadDataFlow;
            _client = client;
        }

        public async Task Execute(object options)
        {
            var dependenciesOptions = (GetDependenciesOptions)options;

            var currentGitBranch = GitHelper.GetCurrentBranchName(_fileSystem.GetWorkingDirectory());
            var configFile = $"{currentGitBranch}.config";
            
            if (string.IsNullOrWhiteSpace(currentGitBranch))
            {
                return;
            }

            _configFullPath = dependenciesOptions.Force
                ? Path.Combine(GetTccDirectory(), configFile)
                : GetConfigFullPath(dependenciesOptions, configFile);

            _dependencyConfig = LoadConfigFile(dependenciesOptions, configFile);

            DependencyConfig dependencyConfig =
                await ResolveDependencies(_dependencyConfig.BuildConfigId, dependenciesOptions.Tag);

            var buildConfig = await _client.BuildConfigs.GetByConfigurationId(_dependencyConfig.BuildConfigId);
            _majorVersion = buildConfig.Parameters[ParameterName.MajorVersion]?.Value;
            _minorVersion = buildConfig.Parameters[ParameterName.MinorVersion]?.Value;

            if (!String.IsNullOrEmpty(_majorVersion) || !String.IsNullOrEmpty(_minorVersion))
            {
                UpdateAssemblyVersion();
                UpdateBuildVersion();
                UpdateVersionIncVersion();
            }

            //only writes the file if changes were made to the config.
            if (_dependencyConfig.Equals(dependencyConfig) == false || dependenciesOptions.Force)
            {
                string json = JsonConvert.SerializeObject(dependencyConfig, Formatting.Indented);
                _fileSystem.EnsureDirectoryExists(_configFullPath);
                _fileSystem.WriteAllTextToFile(_configFullPath, json);
            }

            Log.Info("================ Get Dependencies: done ================");
        }

        public async Task<DependencyConfig> ResolveDependencies(string id, string tag)
        {
            await ResolveDependenciesInternal(id, tag);

            _downloadDataFlow.Complete();

            await _downloadDataFlow.Completion;

            var dependencyConfig = new DependencyConfig
            {
                BuildConfigId = id,
                BuildInfos = _builds.Values.OrderBy(x => x.BuildConfigId).ToList(),
            };

            foreach (var buildInfo in dependencyConfig.BuildInfos)
            {
                Log.Info("Done: {0} - {1}", buildInfo.BuildConfigId, buildInfo.Number);
            }

            return dependencyConfig;
        }

        private async Task ResolveDependenciesInternal(string buildConfigId, string tag)
        {
            Log.Info("Resolving dependencies for: {0}{1}", buildConfigId, String.IsNullOrEmpty(tag) ? "" : ", by \"" + tag + "\" tag");

            BuildConfig buildConfig = await _client.BuildConfigs.GetByConfigurationId(buildConfigId);

            var tasks = buildConfig.ArtifactDependencies.Select(ad => ResolveDependency(ad, tag));

            await Task.WhenAll(tasks);
        }

        private async Task ResolveDependency(DependencyDefinition dependency, string tag)
        {
            Log.Debug("Trying to fetch dependency: {0}", dependency.SourceBuildConfig.Id);

            if (_builds.ContainsKey(dependency.SourceBuildConfig.Id))
            {
                Log.Info("Dependency already fetched. Skipping: {0}", dependency.SourceBuildConfig.Id);
                return;
            }

            Build build;
            if (dependency.Properties.Property["revisionName"].Value == "buildNumber")
            {
                build = await _client.Builds.ByNumber(dependency.Properties.Property["revisionValue"].Value, dependency.SourceBuildConfig.Id);
            }
            else 
            {
                build = await _client.Builds.LastSuccessfulBuildFromConfig(dependency.SourceBuildConfig.Id, tag);
            }

            lock (_builds)
            {
                _builds.Add(build.BuildTypeId, BuildInfo.FromBuild(build));                
            }

            Log.Debug("Downloading artifacts from: {0}-{1}", build.BuildTypeId, build.Number);

            List<ArtifactRule> artifactRules = GetArtifactRules(dependency);

            //create fake files with the reference to the TC resources in order to download.
            List<PathFilePair> files = artifactRules.Select(x => new PathFilePair
            {
                File = x.CreateTeamCityFileReference(build.Href + "/artifacts/content/"),
                Path = Path.Combine(".", x.Dest)
            }).ToList();

            DownloadFiles(files);

            Log.Debug("Done fetching dependency for: {0}", dependency.SourceBuildConfig.Id);
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

        internal DependencyConfig LoadConfigFile(GetDependenciesOptions options, string fileName)
        {
            if (options.Force)
            {
                Log.Debug("Config file not found. Using command line BuildConfigId: {0}", options.BuildConfigId);
                return new DependencyConfig { BuildConfigId = options.BuildConfigId };
            }

            string fullPath = GetConfigFullPath(options, fileName);

            Log.Debug("Loading config file: {0}", fullPath);

            if (_fileSystem.FileExists(fullPath))
            {
                string json = _fileSystem.ReadAllTextFromFile(fullPath);
                return JsonConvert.DeserializeObject<DependencyConfig>(json);
            }
            
            throw new Exception(
                string.Format(
                    "Unable to find {0}. Specify Force option on command line to create the file or provide a proper path using the ConfigFilePath option.",
                    fullPath));
        }

        private string GetConfigFullPath(GetDependenciesOptions options, string fileName)
        {
            string fullPath = GetTccDirectory();

            if (string.IsNullOrEmpty(options.ConfigFilePath) == false)
            {
                fullPath = _fileSystem.GetFullPath(options.ConfigFilePath);
            }

            IEnumerable<string> probingPaths = GetProbingPaths(fullPath, fileName);

            string configPath = probingPaths.FirstOrDefault(x => _fileSystem.FileExists(x));

            //string configPath = Path.Combine(fullPath, fileName);

            if (string.IsNullOrEmpty(configPath) || _fileSystem.FileExists(configPath) == false)
            {
                throw new Exception("Config file not found. From command line use the '-i' option to create a new config file or '-p' to provide a custom path to the file.");
            }

            return configPath;
        }

        private static IEnumerable<string> GetProbingPaths(string directoryName, string fileName)
        {
            IList<string> pathParts = PathHelper.GetPathParts(directoryName);

            for (int i = pathParts.Count; i > 0; i--)
            {
                string path = string.Join(Path.DirectorySeparatorChar.ToString(), pathParts.Take(i));
                yield return path + Path.DirectorySeparatorChar + fileName;
            }
        }

        private string GetSolutionDirectory()
        {
            var solutionDirectoryName = GetRootDirectory() + "\\src";

            return Directory.Exists(solutionDirectoryName) ? solutionDirectoryName : "";
        }

        private string GetRootDirectory()
        {
            var rootDirectoryName = Directory.GetCurrentDirectory();

            return Directory.Exists(rootDirectoryName) ? rootDirectoryName : "";
        }

        private string GetTccDirectory()
        {
            return Path.Combine(_fileSystem.GetWorkingDirectory(), ".tcc");
        }

        private void UpdateAssemblyVersion()
        {
            string path = GetSolutionDirectory() + "\\CommonAssemblyInfo.cs";

            if (System.IO.File.Exists(path))
            {
                Regex regex = new Regex(@"^(\s*\[assembly:\s*AssemblyVersion\s*\("")(\d*\.\d*\.\d*\.\d*)(""\)\]\s*$)",
                    RegexOptions.Multiline);

                System.IO.File.WriteAllText(path,
                    regex.Replace(System.IO.File.ReadAllText(path),
                        $@"${{1}}{_majorVersion}.{_minorVersion}.0.0$3"));
            }
        }

        private void UpdateBuildVersion()
        {
            string rootDirectory = GetRootDirectory();

            if (System.IO.File.Exists(rootDirectory + "\\build.bat"))
            {
                ReplaceBuildVersion(rootDirectory + "\\build.bat");
            }
            if (System.IO.File.Exists(rootDirectory + "\\nonbuild\\build.bat"))
            {
                ReplaceBuildVersion(rootDirectory + "\\nonbuild\\build.bat");
            }
        }

        private void ReplaceBuildVersion(string path)
        {
            Regex regex = new Regex(@"(\s*major_ver=')(\d*)(';\s)(\s*minor_ver=')(\d*)(';\s)");

            System.IO.File.WriteAllText(path,
                regex.Replace(System.IO.File.ReadAllText(path),
                    $@"${{1}}{_majorVersion}$3${{4}}{_minorVersion}$6"));
        }

        private void UpdateVersionIncVersion()
        {
            var rootDirectory = Directory.GetFiles(GetRootDirectory(), "Version.inc", SearchOption.AllDirectories);

            if (!rootDirectory.Any())
                return;

            Regex regexMajor = new Regex(@"(\s*_MAJORNUMBER\s)(\d*)(\s*$)", RegexOptions.Multiline);
            Regex regexMinor = new Regex(@"(\s*_MINORNUMBER\s)(\d*)(\s*$)", RegexOptions.Multiline);

            System.IO.File.WriteAllText(rootDirectory[0],
                regexMajor.Replace(System.IO.File.ReadAllText(rootDirectory[0]),
                    $@"${{1}}{_majorVersion}$3"));

            System.IO.File.WriteAllText(rootDirectory[0],
                regexMinor.Replace(System.IO.File.ReadAllText(rootDirectory[0]),
                    $@"${{1}}{_minorVersion}$3"));
        }

    }

    public class PathFilePair
    {
        public string Path { get; set; }
        public File File { get; set; }
    }
}