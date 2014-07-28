using System.Collections.Generic;

namespace TeamCityApi.Domain
{
    public class BuildWrapper
    {
        public string Count { get; set; }

        public List<BuildSummary> Build { get; set; }
    }
}