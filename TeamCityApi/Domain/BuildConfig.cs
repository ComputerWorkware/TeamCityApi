using System.Collections.Generic;
using Newtonsoft.Json;

namespace TeamCityApi.Domain
{
    public class BuildConfig
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string ProjectName { get; set; }
        public string ProjectId { get; set; }
        public string Href { get; set; }
        public string WebUrl { get; set; }

        public ProjectSummary Project { get; set; }

        public TemplateSummary Template { get; set; }

        [JsonProperty("steps")]
        public List<BuildStep> BuildSteps { get; set; }

        public List<Trigger> Triggers { get; set; }

        [JsonProperty("artifact-dependencies")]
        public List<DependencyDefinition> ArtifactDependencies { get; set; }

        [JsonProperty("snapshot-dependencies")]
        public List<DependencyDefinition> SnapshotDependencies { get; set; }

        public List<Property> Settings { get; set; }
        public List<Property> Parameters { get; set; }

        //TODO: vcs-root-entries
        //TODO: features
        //TODO: agent-requirements
        //TODO: builds

        public override string ToString()
        {
            return string.Format("Id: {0}, Name: {1}, ProjectId: {2}, ProjectName: {3}, Href: {4}, WebUrl: {5}", Id, Name, ProjectId, ProjectName, Href, WebUrl);
        }
    }
}