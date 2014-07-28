using System.Collections.Generic;
using Newtonsoft.Json;

namespace TeamCityApi.Domain
{
    public class Project
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string ParentProjectId { get; set; }
        public string Href { get; set; }
        public string WebUrl { get; set; }

        public ProjectSummary ParentProject { get; set; }

        [JsonProperty("buildTypes")]
        public List<BuildConfigSummary> BuildConfigs { get; set; }

        public List<Property> Properties { get; set; }

        public List<ProjectSummary> Projects { get; set; }

        //TODO: templates
        //TODO: vcsRoots

        public override string ToString()
        {
            return string.Format("Id: {0}, Name: {1}, ParentProjectId: {2}, Href: {3}, WebUrl: {4}", Id, Name, ParentProjectId, Href, WebUrl);
        }
    }
}