using System;
using CommandLine;

namespace TeamCityConsole.Options
{
    [Verb(Verbs.GetDependencies, HelpText = "Download dependencies using the chained artifact dependencies")]
    public class GetDependenciesOptions
    {
        [Option('c', "BuildConfigId", Required = false, HelpText = "Configuration Id")]
        public string BuildConfigId { get; set; }

        [Option('p', "ConfigFilePath", Required = false, HelpText = "Path to the configuration file")]
        public string ConfigFilePath { get; set; }

        [Option('f', "Force", HelpText = "Force creation of configuration file")]
        public bool Force { get; set; }

        public void Validate()
        {
            if (Force && string.IsNullOrEmpty(BuildConfigId))
            {
                throw new Exception("BuildConfigId is required when Force option is specified.");
            }
        }
    }
}