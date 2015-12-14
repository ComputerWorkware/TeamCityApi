namespace TeamCityApi.Domain
{
    public class CreateArtifactDependency
    {
        public CreateArtifactDependency(string targetBuildConfigId, string dependencyBuildConfigId)
        {
            DependencyBuildConfigId = dependencyBuildConfigId;
            TargetBuildConfigId = targetBuildConfigId;

            CleanDestinationDirectory = false;
            RevisionName = "sameChainOrLastFinished";
            RevisionValue = "latest.sameChainOrLastFinished";
            PathRules = "** => ";
        }

        public string TargetBuildConfigId { get; set; }
        public string DependencyBuildConfigId { get; set; }

        public bool CleanDestinationDirectory { get; set; }
        public string PathRules { get; set; }
        public string RevisionName { get; set; }
        public string RevisionValue { get; set; }
        
    }
}