using Newtonsoft.Json;

namespace TeamCityApi.Domain
{
    public class VcsRootInstance
    {
        public long Id { get; set; }
        [JsonProperty("vcs-root-id")]
        public string VcsRootId { get; set; }
        public string Name { get; set; }
        public string VcsName { get; set; }
        public string LastChecked { get; set; }
        public string LastVersion { get; set; }
        public string Href { get; set; }
        [JsonProperty("vcs-root")]
        public VcsRoot VcsRoot { get; set; }
        [JsonProperty("parameters")]
        public Properties Parameters { get; set; }

        public override string ToString()
        {
            return string.Format("Id: {0}, Name: {1}, VcsRootId: {2}, Href: {3}", Id, Name, VcsRootId, Href);
        }
    }
}