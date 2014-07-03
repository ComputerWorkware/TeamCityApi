using Newtonsoft.Json;

namespace TeamCityApi.Domain
{
    public class Revision
    {
        public string Version { get; set; }
        [JsonProperty("vcs-root-instance")]
        public VcsRootInstance VcsRootInstance { get; set; }

        public override string ToString()
        {
            return string.Format("Version: {0}", Version);
        }
    }
}