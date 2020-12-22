using CommandLine;

namespace TeamCityConsole.Options
{
    [Verb(Verbs.DeleteGitBranchesInBuildChain, HelpText = "Deletes git branches in a build chain. ")]
    public class DeleteGitBranchInBuildChainOptions
    {
        [Option('c', "BuildConfigId", Required = true, HelpText = "Configuration Id of the root build configuration.")]
        public string BuildConfigId { get; set; }

        [Option('b', "Branch", Required = true, HelpText = "Branch name to delete.")]
        public string Branch { get; set; }

        [Option('s', "Simulate", Required = false, HelpText = "Simulate the delete function. For real run ommit this parameter.")]
        public bool Simulate { get; set; }
    }
}