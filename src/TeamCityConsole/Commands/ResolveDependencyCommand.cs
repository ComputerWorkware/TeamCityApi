using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
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
    class ResolveDependencyCommand : ICommand
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private readonly ITeamCityClient _client;

        private readonly IFileDownloader _downloader;

        private readonly IFileSystem _fileSystem;

        private const string ConfigFile = "dependencies.config";

        private DependencyConfig _dependencyConfig;

        private readonly Dictionary<string, BuildInfo> _builds = new Dictionary<string, BuildInfo>();

        public ResolveDependencyCommand(ITeamCityClient client, IFileDownloader downloader, IFileSystem fileSystem)
        {
            _downloader = downloader;
            _fileSystem = fileSystem;
            _client = client;
        }

        public async Task Execute(object options)
        {
            var dependenciesOptions = (GetDependenciesOptions)options;

            dependenciesOptions.Validate();

            string configFullPath = GetConfigFullPath(dependenciesOptions, ConfigFile);

            _dependencyConfig = LoadConfigFile(dependenciesOptions, ConfigFile);

            DependencyConfig dependencyConfig = await ResolveDependencies(_dependencyConfig.BuildConfigId);

            //only writes the file if changes were made to the config.
            if (_dependencyConfig.Equals(dependencyConfig) == false || dependenciesOptions.Force)
            {
                string json = JsonConvert.SerializeObject(dependencyConfig, Formatting.Indented);

                _fileSystem.WriteAllTextToFile(configFullPath, json);
            }
        }

        public async Task<DependencyConfig> ResolveDependencies(string id)
        {
            await ResolveDependenciesInternal(id);

            var dependencyConfig = new DependencyConfig
            {
                BuildConfigId = id,
                BuildInfos = _builds.Values.ToList(),
                OutputPath = _dependencyConfig.OutputPath
            };

            return dependencyConfig;
        }

        private async Task ResolveDependenciesInternal(string buildConfigId)
        {
            Log.Info("Resolving dependencies for: {0}", buildConfigId);

            BuildConfig buildConfig = await _client.BuildConfigs.GetByConfigurationId(buildConfigId);

            foreach (var dependency in buildConfig.ArtifactDependencies)
            {
                await ResolveDependency(dependency);
            }

            foreach (var dependency in buildConfig.ArtifactDependencies)
            {
                await ResolveDependenciesInternal(dependency.SourceBuildConfig.Id);
            }
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

            _builds.Add(build.BuildTypeId, BuildInfo.FromBuild(build));

            Log.Debug("Downloading artifacts from: {0}-{1}", build.BuildTypeId, build.Number);

            List<ArtifactRule> artifactRules = GetArtifactRules(dependency);

            //if rules are defined we create fake files with the reference to the TC resources in order to download.
            //List<File> files = artifactRules.Select(x => x.CreateTeamCityFileReference(build.Href + "/artifacts/content/")).ToList();
            List<PathFilePair> files = artifactRules.Select(x => new PathFilePair
            {
                File = x.CreateTeamCityFileReference(build.Href + "/artifacts/content/"),
                Path = x.Dest
            }).ToList();

            await DownloadFiles(_dependencyConfig.OutputPath, files);

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

        private async Task DownloadFiles(string destPath, IEnumerable<PathFilePair> files)
        {
            foreach (PathFilePair file in files)
            {
                if (file.File.HasContent)
                {
                    Log.Debug("Downloading {0} to {1}", file.File.Name, file.Path);
                    await _downloader.Download(file.Path, file.File);
                }
                else
                {
                    List<File> children = await file.File.GetChildren();
                    IEnumerable<PathFilePair> childPairs = children.Select(x => new PathFilePair
                    {
                        File = x,
                        Path = System.IO.Path.Combine(file.Path, x.Name)
                    });
                    await DownloadFiles(destPath, childPairs);
                }
            }
        }

        internal DependencyConfig LoadConfigFile(GetDependenciesOptions options, string fileName)
        {
            string fullPath = GetConfigFullPath(options, fileName);

            Log.Debug("Loading config file: {0}", fullPath);

            if (_fileSystem.FileExists(fullPath))
            {
                string json = _fileSystem.ReadAllTextFromFile(fullPath);
                return JsonConvert.DeserializeObject<DependencyConfig>(json);
            }
            
            if (options.Force)
            {
                Log.Debug("Config file not found. Using command line BuildConfigId: {0}", options.BuildConfigId);
                return new DependencyConfig {BuildConfigId = options.BuildConfigId, OutputPath = options.OutputPath};
            }

            throw new Exception(
                string.Format(
                    "Unable to find {0}. Specify Force option on command line to create the file or provide a proper path using the ConfigFilePath option.",
                    fullPath));
        }

        private static string GetConfigFullPath(GetDependenciesOptions options, string fileName)
        {
            string fullPath = Path.GetFullPath(".");

            if (string.IsNullOrEmpty(options.ConfigFilePath) == false)
            {
                fullPath = Path.GetFullPath(options.ConfigFilePath);
            }

            fullPath = Path.Combine(fullPath, fileName);
            return fullPath;
        }

        private class PathFilePair
        {
            public string Path { get; set; }
            public File File { get; set; }
        }
    }
}