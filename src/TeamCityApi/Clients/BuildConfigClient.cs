using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using TeamCityApi.Domain;
using TeamCityApi.Locators;
using TeamCityApi.Util;

namespace TeamCityApi.Clients
{
    public interface IBuildConfigClient
    {
        Task<List<BuildConfigSummary>> GetAll();
        Task<List<BuildDependency>> GetAllSnapshotDependencies(string buildId);
        Task<BuildConfig> GetByConfigurationId(string buildConfigId);
        Task SetParameterValue(BuildTypeLocator locator, string name, string value);
        Task CreateSnapshotDependency(CreateSnapshotDependency dependency);
        Task CreateArtifactDependency(CreateArtifactDependency dependency);
        Task DeleteSnapshotDependency(string buildConfigId, string dependencyBuildConfigId);
        Task DeleteAllSnapshotDependencies(BuildConfig buildConfig);
        Task CreateDependency(string targetBuildConfigId, DependencyDefinition dependencyDefinition);

        Task<BuildConfig> CopyBuildConfiguration(ProjectLocator destinationProjectLocator, string newConfigurationName,
            BuildTypeLocator sourceBuildTypeLocator, bool copyAllAssociatedSettings = true, bool shareVCSRoots = true);

        Task<BuildConfig> CopyBuildConfigurationFromBuildId(string buildId, string newNameSuffix);
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

        public async Task<List<BuildDependency>> GetAllSnapshotDependencies(string buildId)
        {
            string requestUri = string.Format("/app/rest/builds?locator=snapshotDependency:(to:(id:{0}),includeInitial:true),defaultFilter:false");

            List<BuildDependency> dependencies = await _http.Get<List<BuildDependency>>(requestUri);

            return dependencies;
        }

        public async Task<BuildConfig> GetByConfigurationId(string buildConfigId)
        {
            string requestUri = string.Format("/app/rest/buildTypes/id:{0}", buildConfigId);

            BuildConfig buildConfig = await _http.Get<BuildConfig>(requestUri);

            return buildConfig;
        }

        public async Task SetParameterValue(BuildTypeLocator locator, string name, string value)
        {
            string requestUri = string.Format("/app/rest/buildTypes/{0}/parameters/{1}", locator, name);

            await _http.PutJson(requestUri, Json.Serialize(new Property(){Name = name, Value = value}));
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

        public async Task DeleteAllSnapshotDependencies(BuildConfig buildConfig)
        {
            foreach (DependencyDefinition dependencyDefinition in buildConfig.SnapshotDependencies)
            {
                await DeleteSnapshotDependency(buildConfig.Id, dependencyDefinition.SourceBuildConfig.Id);
            }
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

        public async Task<BuildConfig> CopyBuildConfiguration(ProjectLocator destinationProjectLocator, string newConfigurationName, BuildTypeLocator sourceBuildTypeLocator, bool copyAllAssociatedSettings = true, bool shareVCSRoots = true)
        {
            var xml = CopyBuildConfigurationXml(newConfigurationName, sourceBuildTypeLocator, copyAllAssociatedSettings, shareVCSRoots);

            var url = string.Format("/app/rest/projects/{0}/buildTypes", destinationProjectLocator);

            return await _http.PostXml<BuildConfig>(url, xml);
        }

        private static string CopyBuildConfigurationXml(string newConfigurationName, BuildTypeLocator sourceBuildTypeLocator, bool copyAllAssociatedSettings, bool shareVCSRoots)
        {
            var element = new XElement("newBuildTypeDescription",
                new XAttribute("name", newConfigurationName),
                new XAttribute("sourceBuildTypeLocator", sourceBuildTypeLocator),
                new XAttribute("copyAllAssociatedSettings", copyAllAssociatedSettings),
                new XAttribute("shareVCSRoots", shareVCSRoots)
            );

            return element.ToString();
        }


        public async Task<BuildConfig> CopyBuildConfigurationFromBuildId(string buildId, string newNameSuffix)
        {
            var buildClient = new BuildClient(_http);

            var build = await buildClient.ById(buildId);

            var newBuildConfig = await CopyBuildConfiguration(
                new ProjectLocator().WithId(build.BuildConfig.ProjectId),
                build.BuildConfig.Name + " | " + newNameSuffix,
                new BuildTypeLocator().WithId(build.BuildConfig.Id)
            );

            await DeleteAllSnapshotDependencies(newBuildConfig);

            //todo: freeze artifact dependencies OR REBUILD ARTIFACT DEPENDENCIES, WHICH COULD CHANGE??

            //overwrite parameters, based on source build
            await Task.WhenAll(
                newBuildConfig.Parameters.Select(
                    newP => SetParameterValue(
                        new BuildTypeLocator().WithId(newBuildConfig.Id),
                        newP.Name,
                        build.Properties.Single(oldP => oldP.Name == newP.Name).Value)));

            return newBuildConfig;
        }
    }
}