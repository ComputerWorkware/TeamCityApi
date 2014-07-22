using System.Collections.Generic;
using System.Linq;
using TeamCityApi.Domain;

namespace TeamCityConsole.Commands
{
    public class BuildInfo
    {
        public string Id { get; set; }
        public string Number { get; set; }
        public string BuildConfigId { get; set; }
        public string CommitHash { get; set; }

        public static BuildInfo FromBuild(Build build)
        {
            return new BuildInfo
            {
                Id = build.Id,
                Number = build.Number,
                BuildConfigId = build.BuildTypeId,
                CommitHash = build.Revisions.Any() ? build.Revisions[0].Version : string.Empty
            };
        }

        public override string ToString()
        {
            return string.Format("Id: {0}, Number: {1}, BuildConfigId: {2}", Id, Number, BuildConfigId);
        }
    }

    public class DependencyConfig
    {
        public string BuildConfigId { get; set; }

        public List<BuildInfo> BuildInfos { get; set; }
    }
}