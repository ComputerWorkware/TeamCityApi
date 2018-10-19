using System.Collections.Generic;
using Newtonsoft.Json;

namespace TeamCityApi.Domain
{
    public class Templates
    {
        public int Count { get; set; }

        [JsonProperty("buildType")]
        public List<Template> BuildType { get; set; }
    }
}