using CommandLine;

namespace TeamCityConsole.Options
{
    [Verb(Verbs.CompareBuilds, HelpText = "Compare Builds.")]
    public class CompareBuildsOptions
    {
        [Option('n', "newBuildId", Required = true, HelpText = "New Build Id to compare.")]
        public long NewBuildId { get; set; }

        [Option('o', "oldBuildId", Required = true, HelpText = "Old Build Id to compare.")]
        public long OldBuildId { get; set; }

        [Option('b', "BeyondCompare", Required = false, HelpText = "Display comparison in BayondCompare app.")]
        public bool BCompare { get; set; }

        [Option('d', "Dump", Required = false, HelpText = "Dump the two builds in console (used by TeamCity)")]
        public bool Dump { get; set; }
    }
}