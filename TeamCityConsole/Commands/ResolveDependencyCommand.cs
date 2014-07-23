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

        public ResolveDependencyCommand()
        {
            var http = new HttpClientWrapper(Settings.TeamCityUri, Settings.Username, Settings.Password);
            _downloader = new FileDownloader(http);
            _client = new TeamCityClient(http);
            _fileSystem = new FileSystem();
        }

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

            _dependencyConfig = LoadConfigfile(dependenciesOptions, ConfigFile);

            await ResolveDependencies(_dependencyConfig.BuildConfigId);
        }

        public async Task ResolveDependencies(string id)
        {
            await ResolveDependenciesInternal(id);

            var dependencyConfig = new DependencyConfig
            {
                BuildConfigId = id,
                BuildInfos = _builds.Values.ToList(),
                OutputPath = _dependencyConfig.OutputPath
            };

            if (_dependencyConfig.Equals(dependencyConfig) == false)
            {
                string json = JsonConvert.SerializeObject(dependencyConfig, Formatting.Indented);

                System.IO.File.WriteAllText(ConfigFile, json);               
            }
        }

        private async Task ResolveDependenciesInternal(string buildConfigId)
        {
            Log.Info("Resolving dependencies for: {0}", buildConfigId);

            BuildConfig buildConfig = await _client.BuildConfigs.GetByConfigurationId(buildConfigId);

            foreach (var dependency in buildConfig.ArtifactDependencies)
            {
                await ResolveDependency(dependency);
            }
        }

        private async Task ResolveDependency(DependencyDefinition dependency)
        {
            if (_builds.ContainsKey(dependency.SourceBuildConfig.Id))
            {
                Log.Info("Dependency already fetched. Skipping: {0}", dependency.SourceBuildConfig.Id);
                return;
            }

            Build build = await _client.Builds.LastSuccessfulBuildFromConfig(dependency.SourceBuildConfig.Id);

            _builds.Add(build.BuildTypeId, BuildInfo.FromBuild(build));

            Log.Debug("{0}-{1}", build.BuildTypeId, build.Number);

            List<ArtifactRule> artifactRules = GetArtifactRules(dependency);

            //if rules are defined we create fake files with the reference to the TC resources in order to download.
            List<File> files = artifactRules.Select(x => x.CreateTeamCityFileReference(build.Href + "/artifacts/content/")).ToList();

            await DownloadFiles(_dependencyConfig.OutputPath, files);

            await ResolveDependenciesInternal(build.BuildTypeId);
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

        private async Task DownloadFiles(string destPath, IEnumerable<File> files)
        {
            foreach (File file in files)
            {
                if (file.HasContent)
                {
                    await _downloader.Download(destPath, file);
                }
                else
                {
                    List<File> children = await file.GetChildren();
                    await DownloadFiles(destPath, children);
                }
            }
        }

        private DependencyConfig LoadConfigfile(GetDependenciesOptions options, string fileName)
        {
            string fullPath = Path.GetFullPath(".");

            if (string.IsNullOrEmpty(options.ConfigFilePath) == false)
            {
                fullPath = Path.GetFullPath(options.ConfigFilePath);
            }

            fullPath = Path.Combine(fullPath, fileName);

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
    }
}