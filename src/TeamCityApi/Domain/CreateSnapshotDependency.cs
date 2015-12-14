using TeamCityApi.Util;

namespace TeamCityApi.Domain
{
    public class CreateSnapshotDependency
    {
        public string TargetBuildConfigId { get; private set; }
        public string DependencyBuildConfigId { get; private set; }

        public BuildContinuationMode RunBuildIfDependencyFailed { get; set; }
        public BuildContinuationMode RunBuildIfDependencyFailedToStart { get; set; }
        public bool RunBuildOnTheSameAgent { get; set; }
        public bool TakeStartedBuildWithSameRevisions { get; set; }
        public bool TakeSuccessFulBuildsOnly { get; set; }

        public CreateSnapshotDependency(string targetBuildConfigId, string dependencyBuildConfigId)
        {
            DependencyBuildConfigId = dependencyBuildConfigId;
            TargetBuildConfigId = targetBuildConfigId;

            RunBuildIfDependencyFailed = BuildContinuationMode.MakeFailedToStart;
            RunBuildIfDependencyFailedToStart = BuildContinuationMode.MakeFailedToStart;
            TakeSuccessFulBuildsOnly = true;
            RunBuildOnTheSameAgent = false;
            TakeStartedBuildWithSameRevisions = true;
        }

        public override string ToString()
        {
            return Json.Serialize(this);
        }
    }
}