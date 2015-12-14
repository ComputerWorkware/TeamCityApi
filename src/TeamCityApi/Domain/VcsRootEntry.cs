using Newtonsoft.Json;

namespace TeamCityApi.Domain
{
    public class VcsRootEntry
    {
        public string Id { get; set; }

        [JsonProperty("vcs-root")]
        public VcsRoot VcsRoot { get; set; }

        public string CheckoutRules { get; set; }
    }
}