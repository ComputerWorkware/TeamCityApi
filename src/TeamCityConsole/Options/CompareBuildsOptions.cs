using CommandLine;

namespace TeamCityConsole.Options
{
    [Verb(Verbs.CompareBuilds, HelpText = "Compare Builds.")]
    public class CompareBuildsOptions
    {
        [Option('l', "left", Required = true, HelpText = "Build Id to show on the left.")]
        public string BuildId1 { get; set; }

        [Option('r', "right", Required = true, HelpText = "Build Id to show on the right.")]
        public string BuildId2 { get; set; }
    }
}