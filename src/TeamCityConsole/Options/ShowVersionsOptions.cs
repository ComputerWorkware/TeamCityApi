using CommandLine;

namespace TeamCityConsole.Options
{
    [Verb(Verbs.ShowVersions, HelpText = "Show versions of build configurations in chain.")]
    public class ShowVersionsOptions
    {
        [Option('c', "BuildConfigId", Required = true, HelpText = "Build Config Id of the chain root.")]
        public string BuildConfigId { get; set; }
    }
}