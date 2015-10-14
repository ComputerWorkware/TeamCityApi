using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using TeamCityApi.Domain;

namespace TeamCityApi.Clients
{
    public interface IBuildConfigClient
    {
        Task<List<BuildConfigSummary>> GetAll();
        Task<BuildConfig> GetByConfigurationId(string buildConfigId);
        Task CreateSnapshotDependency(CreateSnapshotDependency dependency);
        Task CreateArtifactDependency(CreateArtifactDependency dependency);
        Task DeleteSnapshotDependency(string buildConfigId, string dependencyBuildConfigId);
        Task CreateDependency(string targetBuildConfigId, DependencyDefinition dependencyDefinition);
    }

    public class BuildConfigClient : IBuildConfigClient
    {
        private readonly IHttpClientWrapper _http;

        public BuildConfigClient(IHttpClientWrapper http)
        {
            _http = http;
        }

        public async Task<List<BuildConfigSummary>> GetAll()
        {
            string requestUri = string.Format("/app/rest/buildTypes");

            List<BuildConfigSummary> buildConfigs = await _http.Get<List<BuildConfigSummary>>(requestUri);

            return buildConfigs;
        }

        public async Task<BuildConfig> GetByConfigurationId(string buildConfigId)
        {
            string requestUri = string.Format("/app/rest/buildTypes/id:{0}", buildConfigId);

            BuildConfig buildConfig = await _http.Get<BuildConfig>(requestUri);

            return buildConfig;
        }

        public async Task CreateSnapshotDependency(CreateSnapshotDependency dependency)
        {
            string requestUri = string.Format("/app/rest/buildTypes/id:{0}", dependency.DependencyBuildConfigId);

            BuildConfigSummary buildConfig = await _http.Get<BuildConfigSummary>(requestUri);

            var dependencyDefinition = new DependencyDefinition
            {
                Id = buildConfig.Id,
                Type = "snapshot_dependency",
                Properties = new List<Property>
                    {
                        new Property() { Name = "run-build-if-dependency-failed", Value = dependency.RunBuildIfDependencyFailed.ToString() },
                        new Property() { Name = "take-successful-builds-only", Value = dependency.TakeSuccessFulBuildsOnly.ToString() },
                        new Property() { Name = "run-build-on-the-same-agent", Value = dependency.RunBuildOnTheSameAgent.ToString() },
                        new Property() { Name = "take-started-build-with-same-revisions", Value = dependency.TakeStartedBuildWithSameRevisions.ToString() },
                    },
                SourceBuildConfig = buildConfig
            };

            await CreateDependency(dependency.TargetBuildConfigId, dependencyDefinition);
        }


        public async Task DeleteSnapshotDependency(string buildConfigId, string dependencyBuildConfigId)
        {
            var url = string.Format("/app/rest/buildTypes/{0}/snapshot-dependencies/{1}", buildConfigId, dependencyBuildConfigId);
            await _http.Delete(url);
        }

        public async Task CreateArtifactDependency(CreateArtifactDependency dependency)
        {
            string requestUri = string.Format("/app/rest/buildTypes/id:{0}", dependency.DependencyBuildConfigId);

            BuildConfigSummary buildConfig = await _http.Get<BuildConfigSummary>(requestUri);

            var dependencyDefinition = new DependencyDefinition
            {
                Id = "0",
                Type = "artifact_dependency",
                Properties = new List<Property>
                    {
                        new Property() { Name = "cleanDestinationDirectory", Value = dependency.CleanDestinationDirectory.ToString() },
                        new Property() { Name = "pathRules", Value = dependency.PathRules },
                        new Property() { Name = "revisionName", Value = dependency.RevisionName },
                        new Property() { Name = "revisionValue", Value = dependency.RevisionValue },
                    },
                SourceBuildConfig = buildConfig
            };

            await CreateDependency(dependency.TargetBuildConfigId, dependencyDefinition);
        }

        public async Task CreateDependency(string targetBuildConfigId, DependencyDefinition dependencyDefinition)
        {
            var xml = CreateDependencyXml(dependencyDefinition);

            var url = string.Format("/app/rest/buildTypes/{0}/{1}-dependencies",targetBuildConfigId, dependencyDefinition.Type.Split('_')[0]);

            await _http.PostXml(url, xml);
        }

        private static string CreateDependencyXml(DependencyDefinition definition)
        {
            var element = new XElement(definition.Type.Replace('_','-'),
                new XAttribute("id", definition.Id),
                new XAttribute("type", definition.Type),
                new XElement("properties", definition.Properties.Select(x => new XElement("property", new XAttribute("name", x.Name), new XAttribute("value", x.Value))).ToArray()),
                    new XElement("source-buildType", new XAttribute("id", definition.SourceBuildConfig.Id),
                        new XAttribute("name", definition.SourceBuildConfig.Name),
                        new XAttribute("href", definition.SourceBuildConfig.Href),
                        new XAttribute("projectName", definition.SourceBuildConfig.ProjectName),
                        new XAttribute("projectId", definition.SourceBuildConfig.ProjectId),
                        new XAttribute("webUrl", definition.SourceBuildConfig.WebUrl))
                );

            return element.ToString();
        }
    }
}