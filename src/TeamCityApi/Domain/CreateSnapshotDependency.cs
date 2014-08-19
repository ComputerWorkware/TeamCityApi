namespace TeamCityApi.Domain
{
    public class CreateSnapshotDependency
    {
        public string TargetBuildConfigId { get; private set; }
        public string DependencyBuildConfigId { get; private set; }

        public bool RunBuildIfDependencyFailed { get; set; }
        public bool RunBuildOnTheSameAgent { get; set; }
        public bool TakeStartedBuildWithSameRevisions { get; set; }
        public bool TakeSuccessFulBuildsOnly { get; set; }

        public CreateSnapshotDependency(string targetBuildConfigId, string dependencyBuildConfigId)
        {
            DependencyBuildConfigId = dependencyBuildConfigId;
            TargetBuildConfigId = targetBuildConfigId;

            RunBuildIfDependencyFailed = false;
            TakeSuccessFulBuildsOnly = true;
            RunBuildOnTheSameAgent = false;
            TakeStartedBuildWithSameRevisions = true;
        }
    }
}