using System.Collections.Generic;
using Newtonsoft.Json;

namespace TeamCityApi.Domain
{
    public class VcsRootEntries
    {
        public string Count { get; set; }

        [JsonProperty("vcs-root-entry")]
        public List<VcsRootEntry> VcsRootEntry { get; set; }

    }
}