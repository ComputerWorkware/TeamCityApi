using CommandLine;

namespace TeamCityConsole.Options
{
    [Verb(Verbs.PropagateVersion, HelpText = "Set versions parameters to all projects in build chain.")]
    public class PropagateVersionOptions
    {
        [Option('c', "BuildConfigId", Required = true, HelpText = "Build Config Id to get version from and propagate to dependencies.")]
        public string BuildConfigId { get; set; }

        [Option('s', "Simulate", Required = false, HelpText = "Simulate the function.")]
        public bool Simulate { get; set; }
    }
}