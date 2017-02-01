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

        [Option('t', "tag", Required = false, HelpText = "Optional tag for pulling artifacts.")]
        public string Tag { get; set; }

        [Option('i', "id", Required = false, HelpText = "Option build Id for pulling artifacts")]
        public int BuildId { get; set; }
    }
}