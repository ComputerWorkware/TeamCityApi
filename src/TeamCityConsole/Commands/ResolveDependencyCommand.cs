using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Newtonsoft.Json;
using NLog;
using TeamCityApi;
using TeamCityApi.Domain;
using TeamCityConsole.Model;
using TeamCityConsole.Options;
using TeamCityConsole.Utils;
using File = TeamCityApi.Domain.File;

namespace TeamCityConsole.Commands
{
    public class ResolveDependencyCommand : ICommand
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private readonly ITeamCityClient _client;

        private readonly IDownloadDataFlow _downloadDataFlow;

        private readonly IFileSystem _fileSystem;

        private const string ConfigFile = "dependencies.config";

        private DependencyConfig _dependencyConfig;

        private readonly Dictionary<string, BuildInfo> _builds = new Dictionary<string, BuildInfo>();

        private string _configFullPath;

        public ResolveDependencyCommand(ITeamCityClient client, IFileSystem fileSystem, IDownloadDataFlow downloadDataFlow)
        {
            _fileSystem = fileSystem;
            _downloadDataFlow = downloadDataFlow;
            _client = client;
        }

        public async Task Execute(object options)
        {
            var dependenciesOptions = (GetDependenciesOptions)options;

            _configFullPath = dependenciesOptions.Force
                ? Path.Combine(_fileSystem.GetWorkingDirectory(), ConfigFile)
                : GetConfigFullPath(dependenciesOptions, ConfigFile);

            _dependencyConfig = LoadConfigFile(dependenciesOptions, ConfigFile);

            DependencyConfig dependencyConfig = await ResolveDependencies(_dependencyConfig.BuildConfigId);

            //only writes the file if changes were made to the config.
            if (_dependencyConfig.Equals(dependencyConfig) == false || dependenciesOptions.Force)
            {
                string json = JsonConvert.SerializeObject(dependencyConfig, Formatting.Indented);

                _fileSystem.WriteAllTextToFile(_configFullPath, json);
            }
        }

        public async Task<DependencyConfig> ResolveDependencies(string id)
        {
            await ResolveDependenciesInternal(id);

            _downloadDataFlow.Complete();

            await _downloadDataFlow.Completion;

            var dependencyConfig = new DependencyConfig
            {
                BuildConfigId = id,
                BuildInfos = _builds.Values.OrderBy(x => x.BuildConfigId).ToList(),
            };

            return dependencyConfig;
        }

        private async Task ResolveDependenciesInternal(string buildConfigId)
        {
            Log.Info("Resolving dependencies for: {0}", buildConfigId);

            BuildConfig buildConfig = await _client.BuildConfigs.GetByConfigurationId(buildConfigId);

            var tasks = buildConfig.ArtifactDependencies.Select(ResolveDependency);

            await Task.WhenAll(tasks);
        }

        private async Task ResolveDependency(DependencyDefinition dependency)
        {
            Log.Debug("Trying to fetch depedency: {0}", dependency.SourceBuildConfig.Id);

            if (_builds.ContainsKey(dependency.SourceBuildConfig.Id))
            {
                Log.Info("Dependency already fetched. Skipping: {0}", dependency.SourceBuildConfig.Id);
                return;
            }

            Build build = await _client.Builds.LastSuccessfulBuildFromConfig(dependency.SourceBuildConfig.Id);

            lock (_builds)
            {
                _builds.Add(build.BuildTypeId, BuildInfo.FromBuild(build));                
            }

            Log.Debug("Downloading artifacts from: {0}-{1}", build.BuildTypeId, build.Number);

            List<ArtifactRule> artifactRules = GetArtifactRules(dependency);

            string basePath = PathHelper.PathRelativeTo(Path.GetDirectoryName(_configFullPath), _fileSystem.GetWorkingDirectory());

            if (string.IsNullOrWhiteSpace(basePath))
            {
                basePath = ".";
            }

            //create fake files with the reference to the TC resources in order to download.
            List<PathFilePair> files = artifactRules.Select(x => new PathFilePair
            {
                File = x.CreateTeamCityFileReference(build.Href + "/artifacts/content/"),
                Path = Path.Combine(basePath, x.Dest)
            }).ToList();

            DownloadFiles(files);

            Log.Debug("Done fetching depedencies for: {0}", dependency.SourceBuildConfig.Id);
        }

        private static List<ArtifactRule> GetArtifactRules(DependencyDefinition dependency)
        {
            Property artifactRulesProperty =
                dependency.Properties.FirstOrDefault(
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
            string fullPath = _fileSystem.GetWorkingDirectory();

            if (string.IsNullOrEmpty(options.ConfigFilePath) == false)
            {
                fullPath = _fileSystem.GetFullPath(options.ConfigFilePath);
            }

            IEnumerable<string> probingPaths = GetProbingPaths(fullPath, fileName);

            string configPath = probingPaths.FirstOrDefault(x => _fileSystem.FileExists(x));

            if (string.IsNullOrEmpty(configPath))
            {
                throw new Exception("Config file not found. From command line use the '-i' option to create a new config file or '-p' to provide a custom path to the file.");
            }

            return configPath;
        }

        private static IEnumerable<string> GetProbingPaths(string directoryName, string fileName)
        {
            IList<string> pathParts = PathHelper.GetPathParts(directoryName);

            for (int i = pathParts.Count ; i > 0 ; i--)
            {
                string path = string.Join(Path.DirectorySeparatorChar.ToString(), pathParts.Take(i));
                yield return path + Path.DirectorySeparatorChar + fileName;
            }
        }

        
    }

    public class PathFilePair
    {
        public string Path { get; set; }
        public File File { get; set; }
    }
}