using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace TeamCityApi.Domain
{
    public class Build
    {
        public string Id { get; set; }
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

        public List<Property> Properties { get; set; }

        [JsonProperty("snapshot-dependencies")]
        public List<Dependency> SnapshotDependecies { get; set; }

        [JsonProperty("artifact-dependencies")]
        public List<Dependency> ArtifactDependencies { get; set; }

        [JsonProperty("artifacts")]
        public ArtifactsReference ArtifactsReference { get; set; }
    }

    public class ArtifactsReference
    {
        private IHttpClientWrapper _http;

        public string Href { get; set; }

        public Lazy<List<File>> Files { get; set; }

        internal void InitHttp(IHttpClientWrapper http)
        {
            _http = http;
        }

        private List<File> GetFiles()
        {
            return null;
        }

        public override string ToString()
        {
            return string.Format("Href: {0}", Href);
        }
    }

    public class File
    {
        public int Size { get; set; }
        public DateTime ModificationTime { get; set; }
        public string Name { get; set; }
        public string Href { get; set; }
        public string ContentHref { get; set; }
    }
}