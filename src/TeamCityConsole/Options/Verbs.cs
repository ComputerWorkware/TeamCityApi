namespace TeamCityConsole.Options
{
    static class Verbs
    {
        public const string GetArtifacts = "get-artifacts";
        public const string GetDependencies = "get-dependencies";
        public const string SelfUpdate = "self-update";
        public const string SetConfig = "set-config";
        public const string CloneRootBuildConfig = "clone-root-build-config";
        public const string CloneChildBuildConfig = "clone-child-build-config";
        public const string DeepCloneBuildConfig = "deep-clone-build-config";
        public const string DeleteClonedBuildChain = "delete-cloned-build-chain";
        public const string DeleteGitBranchesInBuildChain = "delete-git-branches-in-build-chain";
        public const string ShowBuildChain = "show-build-chain";
        public const string CompareBuilds = "compare-builds";
        public const string ShowVersions = "show-versions";
        public const string PropagateVersion = "propagate-version";
        public const string GenerateEscrow = "generate-escrow";
    }
}