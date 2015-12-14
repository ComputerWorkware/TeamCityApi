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
            return string.Format("Id: {0}, VcsRootId: {1}, vcsName: {2}, Href: {3}, Name: {4}, Status: {5}, lastChecked: {6}, lastVersion: {7}", Id, VcsRootId, vcsName, Href, Name, Status, lastChecked, lastVersion);
        }

        public Properties Properties { get; set; }

    }
}