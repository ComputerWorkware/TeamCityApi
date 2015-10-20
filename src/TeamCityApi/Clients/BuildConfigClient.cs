using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using TeamCityApi.Domain;
using TeamCityApi.Helpers;
using TeamCityApi.Locators;
using TeamCityApi.Logging;
using TeamCityApi.UseCases;
using TeamCityApi.Util;

namespace TeamCityApi.Clients
{
    public interface IBuildConfigClient
    {
        Task<List<BuildConfigSummary>> GetAll();
        Task<List<BuildDependency>> GetAllSnapshotDependencies(string buildId);
        Task<BuildConfig> GetByConfigurationId(string buildConfigId);
        Task SetParameterValue(Action<BuildTypeLocator> buildTypeLocatorConfig, string name, string value, bool own = true);
        Task CreateSnapshotDependency(CreateSnapshotDependency dependency);
        Task CreateArtifactDependency(CreateArtifactDependency dependency);
        Task DeleteSnapshotDependency(string buildConfigId, string dependencyBuildConfigId);
        Task DeleteAllSnapshotDependencies(BuildConfig buildConfig, HashSet<string> buildConfigIdsToSkip = null);
        Task FreezeAllArtifactDependencies(BuildConfig targetBuildConfig, Build asOfbuild, HashSet<string> buildConfigIdsToSkip = null);
        Task CreateDependency(string targetBuildConfigId, DependencyDefinition dependencyDefinition);
        Task UpdateArtifactDependency(string buildConfigId, DependencyDefinition artifactDependency);

        Task<BuildConfig> CopyBuildConfiguration(Action<ProjectLocator> destinationProjectLocatorConfig, string newConfigurationName,
            Action<BuildTypeLocator> sourceBuildTypeLocatorConfig, bool copyAllAssociatedSettings = true, bool shareVCSRoots = true);

        Task FreezeParameters(Action<BuildTypeLocator> buildTypeLocatorConfig, List<Property> targetParameters, List<Property> sourceParameters);
    }

    public class BuildConfigClient : IBuildConfigClient
    {
        private static readonly ILog Log = LogProvider.GetLogger(typeof(BuildConfigClient));
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
            Log.TraceFormat("API BuildConfig.GetAllSnapshotDependencies(). buildId: {0}", buildId);

            string requestUri = string.Format("/app/rest/builds?locator=snapshotDependency:(to:(id:{0}),includeInitial:true),defaultFilter:false");

            List<BuildDependency> dependencies = await _http.Get<List<BuildDependency>>(requestUri);

            return dependencies;
        }

        public async Task<BuildConfig> GetByConfigurationId(string buildConfigId)
        {
            Log.TraceFormat("API BuildConfig.GetByConfigurationId(). buildConfigId: {0}", buildConfigId);

            string requestUri = string.Format("/app/rest/buildTypes/id:{0}", buildConfigId);

            BuildConfig buildConfig = await _http.Get<BuildConfig>(requestUri);

            return buildConfig;
        }

        public async Task SetParameterValue(Action<BuildTypeLocator> buildTypeLocatorConfig, string name, string value, bool own = true)
        {
            var locator = new BuildTypeLocator();
            buildTypeLocatorConfig(locator);

            Log.TraceFormat("API BuildConfig.SetParameterValue(). buildTypeLocator: {0}, name: {1}, value: {2}, own: {3}", locator, name, value, own);

            string requestUri = string.Format("/app/rest/buildTypes/{0}/parameters/{1}", locator, name);

            await _http.PutJson(requestUri, Json.Serialize(new Property(){Name = name, Value = value, Own = own}));
        }

        public async Task CreateSnapshotDependency(CreateSnapshotDependency dependency)
        {
            Log.DebugFormat("API BuildConfig.CreateSnapshotDependency(). dependency: {0}", dependency);

            string requestUri = string.Format("/app/rest/buildTypes/id:{0}", dependency.DependencyBuildConfigId);

            BuildConfigSummary buildConfig = await _http.Get<BuildConfigSummary>(requestUri);

            var dependencyDefinition = new DependencyDefinition
            {
                Id = buildConfig.Id,
                Type = "snapshot_dependency",
                Properties = new Properties
                {
                    Property = new PropertyList
                    {
                        new Property() { Name = "run-build-if-dependency-failed", Value = dependency.RunBuildIfDependencyFailed.ToString() },
                        new Property() { Name = "run-build-if-dependency-failed-to-start", Value = dependency.RunBuildIfDependencyFailed.ToString() },
                        new Property() { Name = "take-successful-builds-only", Value = dependency.TakeSuccessFulBuildsOnly.ToString() },
                        new Property() { Name = "run-build-on-the-same-agent", Value = dependency.RunBuildOnTheSameAgent.ToString() },
                        new Property() { Name = "take-started-build-with-same-revisions", Value = dependency.TakeStartedBuildWithSameRevisions.ToString() },
                    }
                },
                SourceBuildConfig = buildConfig
            };

            await CreateDependency(dependency.TargetBuildConfigId, dependencyDefinition);
        }


        public async Task DeleteSnapshotDependency(string buildConfigId, string dependencyBuildConfigId)
        {
            Log.TraceFormat("API BuildConfig.DeleteSnapshotDependency(). buildConfigId: {0}, buildConfigId: {1}", buildConfigId, dependencyBuildConfigId);

            var url = string.Format("/app/rest/buildTypes/{0}/snapshot-dependencies/{1}", buildConfigId, dependencyBuildConfigId);
            await _http.Delete(url);
        }

        public async Task DeleteAllSnapshotDependencies(BuildConfig buildConfig, HashSet<string> buildConfigIdsToSkip = null)
        {
            Log.DebugFormat("API BuildConfig.DeleteAllSnapshotDependencies(). buildConfig: {0}, buildConfigIdsToSkip: {1}", buildConfig, buildConfigIdsToSkip);

            foreach (DependencyDefinition dependencyDefinition in buildConfig.SnapshotDependencies)
            {
                if (buildConfigIdsToSkip != null && buildConfigIdsToSkip.Contains(dependencyDefinition.SourceBuildConfig.Id))
                    continue;

                await DeleteSnapshotDependency(buildConfig.Id, dependencyDefinition.SourceBuildConfig.Id);
            }
        }

        public async Task FreezeAllArtifactDependencies(BuildConfig targetBuildConfig, Build asOfbuild, HashSet<string> buildConfigIdsToSkip = null)
        {
            Log.DebugFormat("API BuildConfig.FreezeAllArtifactDependencies(). targetBuildConfig: {0}, asOfbuild: {1}, buildConfigIdsToSkip: {2} ", targetBuildConfig, asOfbuild, buildConfigIdsToSkip);

            foreach (var artifactDependency in targetBuildConfig.ArtifactDependencies)
            {
                if (buildConfigIdsToSkip != null && buildConfigIdsToSkip.Contains(artifactDependency.SourceBuildConfig.Id))
                    continue;

                var buildNumber = asOfbuild.ArtifactDependencies.FirstOrDefault(a => a.BuildTypeId == artifactDependency.SourceBuildConfig.Id).Number;
                artifactDependency.Properties.Property["revisionName"].Value = "buildNumber";
                artifactDependency.Properties.Property["revisionValue"].Value = buildNumber;
                await UpdateArtifactDependency(targetBuildConfig.Id, artifactDependency);
            }
        }

        public async Task UpdateArtifactDependency(string buildConfigId, DependencyDefinition artifactDependency)
        {
            Log.TraceFormat("API BuildConfig.UpdateArtifactDependency(). buildConfigId: {0}, artifactDependency: {1} ", buildConfigId, artifactDependency);

            var url = string.Format("/app/rest/buildTypes/id:{0}/artifact-dependencies/{1}", buildConfigId, artifactDependency.Id);
            await _http.PutJson(url, Json.Serialize(artifactDependency));
        }

        public async Task CreateArtifactDependency(CreateArtifactDependency dependency)
        {
            Log.DebugFormat("API BuildConfig.CreateArtifactDependency(). dependency: {0}", dependency);

            string requestUri = string.Format("/app/rest/buildTypes/id:{0}", dependency.DependencyBuildConfigId);

            BuildConfigSummary buildConfig = await _http.Get<BuildConfigSummary>(requestUri);

            var dependencyDefinition = new DependencyDefinition
            {
                Id = "0",
                Type = "artifact_dependency",
                Properties = new Properties
                {
                    Property = new PropertyList
                    {
                        new Property() { Name = "cleanDestinationDirectory", Value = dependency.CleanDestinationDirectory.ToString() },
                        new Property() { Name = "pathRules", Value = dependency.PathRules },
                        new Property() { Name = "revisionName", Value = dependency.RevisionName },
                        new Property() { Name = "revisionValue", Value = dependency.RevisionValue },
                    }
                }, 
                SourceBuildConfig = buildConfig
            };

            await CreateDependency(dependency.TargetBuildConfigId, dependencyDefinition);
        }

        public async Task CreateDependency(string targetBuildConfigId, DependencyDefinition dependencyDefinition)
        {
            Log.TraceFormat("API BuildConfig.CreateDependency(). targetBuildConfigId: {0}, dependencyDefinition: {1}", targetBuildConfigId, dependencyDefinition);

            var xml = CreateDependencyXml(dependencyDefinition);

            var url = string.Format("/app/rest/buildTypes/{0}/{1}-dependencies",targetBuildConfigId, dependencyDefinition.Type.Split('_')[0]);

            await _http.PostXml(url, xml);
        }

        private static string CreateDependencyXml(DependencyDefinition definition)
        {
            var element = new XElement(definition.Type.Replace('_','-'),
                new XAttribute("id", definition.Id),
                new XAttribute("type", definition.Type),
                new XElement("properties", definition.Properties.Property.Select(x => new XElement("property", new XAttribute("name", x.Name), new XAttribute("value", x.Value))).ToArray()),
                    new XElement("source-buildType", new XAttribute("id", definition.SourceBuildConfig.Id),
                        new XAttribute("name", definition.SourceBuildConfig.Name),
                        new XAttribute("href", definition.SourceBuildConfig.Href),
                        new XAttribute("projectName", definition.SourceBuildConfig.ProjectName),
                        new XAttribute("projectId", definition.SourceBuildConfig.ProjectId),
                        new XAttribute("webUrl", definition.SourceBuildConfig.WebUrl))
                );

            return element.ToString();
        }

        public async Task<BuildConfig> CopyBuildConfiguration(Action<ProjectLocator> destinationProjectLocatorConfig, string newConfigurationName, Action<BuildTypeLocator> sourceBuildTypeLocatorConfig, bool copyAllAssociatedSettings = true, bool shareVCSRoots = true)
        {
            var destinationProjectLocator = new ProjectLocator();
            destinationProjectLocatorConfig(destinationProjectLocator);

            var sourceBuildTypeLocator = new BuildTypeLocator();
            sourceBuildTypeLocatorConfig(sourceBuildTypeLocator);

            Log.TraceFormat("API BuildConfig.CopyBuildConfiguration(). destinationProjectLocator: {0}, newConfigurationName: {1}, sourceBuildTypeLocator: {2}, copyAllAssociatedSettings: {3}, shareVCSRoots: {4}", destinationProjectLocator, newConfigurationName, sourceBuildTypeLocator, copyAllAssociatedSettings, shareVCSRoots);

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

        public async Task FreezeParameters(Action<BuildTypeLocator> buildTypeLocatorConfig, List<Property> targetParameters, List<Property> sourceParameters)
        {
            var buildTypeLocator = new BuildTypeLocator();
            buildTypeLocatorConfig(buildTypeLocator);

            Log.TraceFormat("API BuildConfig.FreezeParameters(). buildTypeLocator: {0}, targetParameters: {1}, sourceParameters: {2}", buildTypeLocator, targetParameters, sourceParameters);

            //1st pass: set different, then in a project, value. Just to make parameter "own", see more: https://youtrack.jetbrains.com/issue/TW-42811
            await Task.WhenAll(
                targetParameters.Select(
                    targetP => SetParameterValue(
                        buildTypeLocatorConfig,
                        targetP.Name,
                        "Temporary value, different from the parent project value!")));

            //2ns pass: set real value.
            await Task.WhenAll(
                targetParameters.Select(
                    targetP => SetParameterValue(
                        buildTypeLocatorConfig,
                        targetP.Name,
                        sourceParameters.Single(sourceP => sourceP.Name == targetP.Name).Value)));
        }
    }
}