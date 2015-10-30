using System;
using System.Collections.Generic;
using System.Diagnostics;
using Newtonsoft.Json;

namespace TeamCityApi.Domain
{
    [DebuggerDisplay("Build: {Id}, {Number}")]
    public class Build
    {
        public long Id { get; set; }
        public string Number { get; set; }
        public string Status { get; set; }
        public string StatusText { get; set; }
        public string State { get; set; }
        public string BuildTypeId { get; set; }
        public string Href { get; set; }
        public string WebUrl { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime FinishDate { get; set; }

        [JsonProperty("buildType")]
        public BuildConfigSummary BuildConfig { get; set; }

        public AgentSummary Agent { get; set; }

        public List<Revision> Revisions { get; set; }

        public List<ChangeSummary> LastChanges { get; set; }

        public Properties Properties { get; set; }

        [JsonProperty("snapshot-dependencies")]
        public List<Dependency> SnapshotDependecies { get; set; }

        [JsonProperty("artifact-dependencies")]
        public List<Dependency> ArtifactDependencies { get; set; }

        [JsonProperty("artifacts")]
        public ArtifactsReference ArtifactsReference { get; set; }

        public override bool Equals(object obj)
        {
            // If parameter is null return false.
            if (obj == null)
            {
                return false;
            }

            // If parameter cannot be cast to Build return false.
            Build b = obj as Build;
            if (b == null)
            {
                return false;
            }

            // Return true if the fields match:
            return (Id == b.Id);
        }

        public bool Equals(Build b)
        {
            // If parameter is null return false:
            if (b == null)
            {
                return false;
            }

            // Return true if the fields match:
            return Id == b.Id;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
    }
}