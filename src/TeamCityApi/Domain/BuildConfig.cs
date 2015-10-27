using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Newtonsoft.Json;
using TeamCityApi.Locators;

namespace TeamCityApi.Domain
{
    [DebuggerDisplay("Build Config: {Id}")]
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

        [JsonProperty("settings")]
        public Properties Settings { get; set; }

        [JsonProperty("parameters")]
        public Properties Parameters { get; set; }

        //TODO: vcs-root-entries
        //TODO: features
        //TODO: agent-requirements
        //TODO: builds


        public bool HasFixedArtifactDependency()
        {
            if (ArtifactDependencies != null)
            {
                return
                    ArtifactDependencies.Any(
                        ad => ad.Properties.Property.Any(p => p.Name == "revisionName" && p.Value == "buildNumber"));
            }
            return false;
        }

        public static implicit operator Action<BuildTypeLocator>(BuildConfig bc)
        {
            return locator => locator.WithId(bc.Id);
        }

        public override string ToString()
        {
            return string.Format("Id: {0}, Name: {1}, ProjectId: {2}, ProjectName: {3}, Href: {4}, WebUrl: {5}", Id, Name, ProjectId, ProjectName, Href, WebUrl);
        }

        public override bool Equals(object obj)
        {
            // If parameter is null return false.
            if (obj == null)
            {
                return false;
            }

            // If parameter cannot be cast to Build return false.
            BuildConfig bc = obj as BuildConfig;
            if (bc == null)
            {
                return false;
            }

            // Return true if the fields match:
            return (Id == bc.Id);
        }

        public bool Equals(BuildConfig bc)
        {
            // If parameter is null return false:
            if (bc == null)
            {
                return false;
            }

            // Return true if the fields match:
            return Id == bc.Id;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        public static string NewName(string oldName, string suffix)
        {
            var index = oldName.LastIndexOf(Consts.SuffixSeparator, StringComparison.Ordinal);
            if (index > 0)
                oldName = oldName.Substring(0, index);

            return oldName + Consts.SuffixSeparator + suffix;
        }

        public static explicit operator BuildConfigSummary(BuildConfig buildConfig)
        {
            return new BuildConfigSummary()
            {
                Href = buildConfig.Href,
                Id = buildConfig.Id,
                ProjectId = buildConfig.ProjectId,
                Name = buildConfig.Name,
                ProjectName = buildConfig.ProjectName,
                WebUrl = buildConfig.WebUrl
            };
        }
    }
}