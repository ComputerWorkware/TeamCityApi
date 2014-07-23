using CommandLine;

namespace TeamCityConsole.Options
{
    [Verb(Verbs.GetArtifacts, HelpText = "Download artifacts")]
    class GetArtifactOptions
    {
        [Option('c', "BuildConfigId", Required = true, HelpText = "Configuration Id")]
        public string BuildConfigId { get; set; }

        [Option('o', "OutputDir", DefaultValue = "", HelpText = "Output directory for downloaded files")]
        public string OutputDirectory { get; set; }
    }
}