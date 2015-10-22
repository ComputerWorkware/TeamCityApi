using CommandLine;

namespace TeamCityConsole.Options
{
    [Verb(Verbs.DeleteBuildChain, HelpText = "Deletes build chain.")]
    public class DeleteBuildChainOptions
    {
        [Option('c', "BuildConfigId", Required = true, HelpText = "Configuration Id.")]
        public string BuildConfigId { get; set; }

        [Option('s', "Simulate", Required = false, HelpText = "Simulate the delete function.")]
        public bool Simulate { get; set; }
    }
}