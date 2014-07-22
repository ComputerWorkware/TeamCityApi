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

    [Verb(Verbs.GetDependencies, HelpText = "Download dependencies using the chained artifact dependencies")]
    class GetDependenciesOptions
    {
        [Option('c', "BuildConfigId", Required = false, HelpText = "Configuration Id")]
        public string BuildConfigId { get; set; }

        [Option('f', "ConfigFile", Required = true, HelpText = "Path to the configuration file")]
        public string ConfigFile { get; set; }
    }
}