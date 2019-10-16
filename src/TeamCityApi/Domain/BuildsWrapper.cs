using System.Collections.Generic;

namespace TeamCityApi.Domain
{
    public class BuildsWrapper
    {
        public string Count { get; set; }

        public List<Build> Build { get; set; }
    }
}