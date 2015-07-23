using System;
using CommandLine;

namespace TeamCityConsole.Options
{
    [Verb(Verbs.GetDependencies, HelpText = "Download dependencies using the chained artifact dependencies")]
    public class GetDependenciesOptions
    {
        [Option('i', "init", Required = false, HelpText = "Specify the build configuration id used to initializ a new dependencies.config")]
        public string BuildConfigId { get; set; }

        [Option('p', "ConfigFilePath", Required = false, HelpText = "Path to the configuration file")]
        public string ConfigFilePath { get; set; }

        [Option('t', "tag", Required = false, HelpText = "Optional tag for pulling dependencies.")]
        public string Tag { get; set; }

        public bool Force
        {
            get { return string.IsNullOrWhiteSpace(BuildConfigId) == false; }
        }
    }
}