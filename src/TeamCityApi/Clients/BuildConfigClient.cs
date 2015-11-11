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
        Task DeleteBuildConfig(string buildConfigId);
        Task FreezeAllArtifactDependencies(BuildConfig targetBuildConfig, Build asOfbuild, HashSet<string> buildConfigIdsToSkip = null);
        Task CreateDependency(string targetBuildConfigId, DependencyDefinition dependencyDefinition);
        Task UpdateArtifactDependency(string buildConfigId, DependencyDefinition artifactDependency);

        Task<BuildConfig> CopyBuildConfiguration(string destinationProjectId, string newConfigurationName,
            string sourceBuildTypeId, bool copyAllAssociatedSettings = true, bool shareVCSRoots = true);

        Task FreezeParameters(Action<BuildTypeLocator> buildTypeLocatorConfig, List<Property> targetParameters, List<Property> sourceParameters);

        Task<string> GenerateUniqueBuildConfigId(string name);
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
            string requestUri = "/app/rest/buildTypes";

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

        public async Task SetParameterValue(Action<BuildTypeLocator> buildTypeLocatorConfig, string name, string value, bool own = true)
        {
            var locator = new BuildTypeLocator();
            buildTypeLocatorConfig(locator);

            Log.TraceFormat("API BuildConfig.SetParameterValue for: {0}, {1}: {2}", locator, name, value);

            string requestUri = string.Format("/app/rest/buildTypes/{0}/parameters/{1}", locator, name);

            await _http.PutJson(requestUri, Json.Serialize(new Property(){Name = name, Value = value, Own = own}));
        }

        public async Task CreateSnapshotDependency(CreateSnapshotDependency dependency)
        {
            Log.DebugFormat("API BuildConfig.CreateSnapshotDependency for: {0}, to: {0}", dependency.TargetBuildConfigId, dependency.DependencyBuildConfigId);

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
            Log.TraceFormat("API BuildConfig.DeleteSnapshotDependency for: {0}, to: {1}", buildConfigId, dependencyBuildConfigId);

            var url = string.Format("/app/rest/buildTypes/{0}/snapshot-dependencies/{1}", buildConfigId, dependencyBuildConfigId);
            await _http.Delete(url);
        }

        public async Task DeleteAllSnapshotDependencies(BuildConfig buildConfig, HashSet<string> buildConfigIdsToSkip = null)
        {
            Log.DebugFormat("API BuildConfig.DeleteAllSnapshotDependencies for: {0}", buildConfig.Id);

            foreach (DependencyDefinition dependencyDefinition in buildConfig.SnapshotDependencies)
            {
                if (buildConfigIdsToSkip != null && buildConfigIdsToSkip.Contains(dependencyDefinition.SourceBuildConfig.Id))
                    continue;

                await DeleteSnapshotDependency(buildConfig.Id, dependencyDefinition.SourceBuildConfig.Id);
            }
        }

        public async Task DeleteBuildConfig(string buildConfigId)
        {
            Log.TraceFormat("API BuildConfig.DeleteBuildConfig for: {0}", buildConfigId);

            var url = string.Format("/app/rest/buildTypes/id:{0}", buildConfigId);
            await _http.Delete(url);
        }
        
        public async Task FreezeAllArtifactDependencies(BuildConfig targetBuildConfig, Build asOfbuild, HashSet<string> buildConfigIdsToSkip = null)
        {
            Log.DebugFormat("API BuildConfig.FreezeAllArtifactDependencies for {0}, asOfbuild: {1}", targetBuildConfig.Id, asOfbuild.Id);

            foreach (var artifactDependency in targetBuildConfig.ArtifactDependencies)
            {
                if (buildConfigIdsToSkip != null && buildConfigIdsToSkip.Contains(artifactDependency.SourceBuildConfig.Id))
                    continue;

                if (asOfbuild.ArtifactDependencies == null)
                {
                    throw new Exception(String.Format("Artifact dependencies for Build #{0} (id: {1}) unexpectedly empty", asOfbuild.Number, asOfbuild.Id));
                }

                var buildNumber = asOfbuild.ArtifactDependencies.FirstOrDefault(a => a.BuildTypeId == artifactDependency.SourceBuildConfig.Id).Number;
                artifactDependency.Properties.Property["revisionName"].Value = "buildNumber";
                artifactDependency.Properties.Property["revisionValue"].Value = buildNumber;
                await UpdateArtifactDependency(targetBuildConfig.Id, artifactDependency);
            }
        }

        public async Task UpdateArtifactDependency(string buildConfigId, DependencyDefinition artifactDependency)
        {
            Log.TraceFormat("API BuildConfig.UpdateArtifactDependency for: {0}, artifactDependency: {{{1}}}", buildConfigId, artifactDependency);

            var url = string.Format("/app/rest/buildTypes/id:{0}/artifact-dependencies/{1}", buildConfigId, artifactDependency.Id);
            await _http.PutJson(url, Json.Serialize(artifactDependency));
        }

        public async Task CreateArtifactDependency(CreateArtifactDependency dependency)
        {
            Log.DebugFormat("API BuildConfig.CreateArtifactDependency for: {0}, to: {1}", dependency.TargetBuildConfigId, dependency.DependencyBuildConfigId);

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
            var xml = CreateDependencyXml(dependencyDefinition);

            Log.TraceFormat("API BuildConfig.CreateDependency for: {0}, dependency: {1}", targetBuildConfigId, xml);

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

        public async Task<BuildConfig> CopyBuildConfiguration(string destinationProjectId, string newConfigurationName, string sourceBuildTypeId, bool copyAllAssociatedSettings = true, bool shareVCSRoots = true)
        {
            Log.TraceFormat("API BuildConfig.CopyBuildConfiguration {0} as \"{1}\"", sourceBuildTypeId, newConfigurationName);

            var xml = CopyBuildConfigurationXml(newConfigurationName, sourceBuildTypeId, copyAllAssociatedSettings, shareVCSRoots);

            var url = string.Format("/app/rest/projects/{0}/buildTypes", new ProjectLocator().WithId(destinationProjectId));
            
            return await _http.PostXml<BuildConfig>(url, xml);
        }

        private static string CopyBuildConfigurationXml(string newConfigurationName, string sourceBuildTypeId, bool copyAllAssociatedSettings, bool shareVCSRoots)
        {
            var element = new XElement("newBuildTypeDescription",
                new XAttribute("name", newConfigurationName),
                new XAttribute("sourceBuildTypeLocator", new BuildTypeLocator().WithId(sourceBuildTypeId)),
                new XAttribute("copyAllAssociatedSettings", copyAllAssociatedSettings),
                new XAttribute("shareVCSRoots", shareVCSRoots)
            );

            return element.ToString();
        }

        public async Task FreezeParameters(Action<BuildTypeLocator> buildTypeLocatorConfig, List<Property> targetParameters, List<Property> sourceParameters)
        {
            var buildTypeLocator = new BuildTypeLocator();
            buildTypeLocatorConfig(buildTypeLocator);

            Log.TraceFormat("API BuildConfig.FreezeParameters for {0}, from: {1}, to: {2}", buildTypeLocator, targetParameters, sourceParameters);

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

        public Task<string> GenerateUniqueBuildConfigId(string name)
        {
            //todo: strip non alpha numeric chars
            //todo: check if teamcity already has made up id
            //todo: if yes append counter until will find unique
            throw new NotImplementedException();
        }
    }
}