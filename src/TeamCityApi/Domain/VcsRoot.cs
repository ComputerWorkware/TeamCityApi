using System;
using Newtonsoft.Json;

namespace TeamCityApi.Domain
{
    public class VcsRoot
    {
        public string Id { get; set; }

        [JsonProperty("vcs-root-id")]
        public string VcsRootId { get; set; }

        public string vcsName { get; set; }
        public string Href { get; set; }
        public string Name { get; set; }
        public string Status { get; set; }
        public DateTime lastChecked { get; set; }
        public string lastVersion { get; set; }

        public override string ToString()
        {
            return Name;
        }

        public Properties Properties { get; set; }

    }
}