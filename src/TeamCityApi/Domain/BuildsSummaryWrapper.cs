using System.Collections.Generic;

namespace TeamCityApi.Domain
{
    public class BuildsSummaryWrapper
    {
        public string Count { get; set; }

        public List<BuildSummary> Build { get; set; }
    }
}