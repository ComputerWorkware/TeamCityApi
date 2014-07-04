using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NLog;
using TeamCityApi;
using TeamCityApi.Domain;
using TeamCityConsole.Options;
using TeamCityConsole.Utils;

namespace TeamCityConsole.Commands
{
    class ResolveDependencyCommand : ICommand
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private readonly TeamCityClient _client;

        private readonly IFileDownloader _downloader;

        private const string ConfigFile = "dependencies.config";

        private readonly Dictionary<string, BuildInfo> _builds = new Dictionary<string, BuildInfo>();

        public ResolveDependencyCommand()
        {
            var http = new HttpClientWrapper(AppSettings.Default.teamcityuri, AppSettings.Default.username, AppSettings.Default.password);
            _downloader = new FileDownloader(http);
            _client = new TeamCityClient(http);
        }

        public ResolveDependencyCommand(IFileDownloader downloader)
        {
            _downloader = downloader;
            _client = new TeamCityClient(AppSettings.Default.teamcityuri, AppSettings.Default.username, AppSettings.Default.password);
        }

        public async Task Execute(object options)
        {
            var dependenciesOptions = options as GetDependenciesOptions;

            await ResolveDependencies(dependenciesOptions.ConfigTypeId);
        }

        public async Task ResolveDependencies(string id)
        {
            await ResolveDependenciesInternal(id);

            string json = JsonConvert.SerializeObject(_builds.Values.ToList(), Newtonsoft.Json.Formatting.Indented);

            System.IO.File.WriteAllText(ConfigFile, json);
        }

        private async Task ResolveDependenciesInternal(string buildConfigId)
        {
            Log.Info("Resolving dependencies for: {0}", buildConfigId);

            BuildConfig buildConfig = await _client.BuildConfigs.GetByConfigurationId(buildConfigId);

            List<DependencyDefinition> artifactDependencies = buildConfig.ArtifactDependencies;

            foreach (var dependency in artifactDependencies)
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

            var build = await _client.Builds.LastSuccessfulBuildFromConfig(dependency.SourceBuildConfig.Id);

            _builds.Add(build.BuildTypeId, BuildInfo.FromBuild(build));

            Log.Debug("{0}-{1}", build.BuildTypeId, build.Number);

            List<File> files = await build.ArtifactsReference.GetFiles();

            await DownloadFiles("assemblies", files);

            await ResolveDependenciesInternal(build.BuildTypeId);
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
    }
}