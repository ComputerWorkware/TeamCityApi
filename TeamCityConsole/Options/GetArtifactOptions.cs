using CommandLine;

namespace TeamCityConsole.Options
{
    [Verb(Verbs.GetArtifacts, HelpText = "Download artifacts")]
    class GetArtifactOptions
    {
        [Option('c', "ConfigId", Required = true, HelpText = "Configuration Id")]
        public string ConfigTypeId { get; set; }

        [Option('o', "OutputDir", DefaultValue = "", HelpText = "Output directory for downloaded files")]
        public string OutputDirectory { get; set; }
    }

    [Verb(Verbs.GetDependencies, HelpText = "Download dependencies using the chained artifact dependencies")]
    class GetDependenciesOptions
    {
        [Option('c', "ConfigId", Required = true, HelpText = "Configuration Id")]
        public string ConfigTypeId { get; set; }
    }
}