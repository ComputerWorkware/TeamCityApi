using CommandLine;

namespace TeamCityConsole.Options
{
    [Verb(Verbs.DeleteClonedBuildChain, HelpText = "Deletes cloned build chain.")]
    public class DeleteClonedBuildChainOptions
    {
        [Option('c', "BuildConfigId", Required = true, HelpText = "Configuration Id.")]
        public string BuildConfigId { get; set; }

        [Option('s', "Simulate", Required = false, HelpText = "Simulate the delete function.")]
        public bool Simulate { get; set; }
    }
}