using CommandLine;

namespace TeamCityConsole.Options
{
    [Verb(Verbs.CompareBuilds, HelpText = "Compare Builds.")]
    public class CompareBuildsOptions
    {
        [Option('o', "oldBuildId", Required = true, HelpText = "Old Build Id to compare.")]
        public string BuildId1 { get; set; }

        [Option('n', "newBuildId", Required = true, HelpText = "New Build Id to compare.")]
        public string BuildId2 { get; set; }
    }
}