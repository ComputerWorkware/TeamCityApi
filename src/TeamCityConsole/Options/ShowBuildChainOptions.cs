using CommandLine;

namespace TeamCityConsole.Options
{
    [Verb(Verbs.ShowBuildChain, HelpText = "Shows build chain.")]
    public class ShowBuildChainOptions
    {
        [Option('c', "BuildConfigId", Required = true, HelpText = "Configuration Id.")]
        public string BuildConfigId { get; set; }
    }
}