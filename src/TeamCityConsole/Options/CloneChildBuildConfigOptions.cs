using System;
using System.Collections.Generic;
using CommandLine;

namespace TeamCityConsole.Options
{
    [Verb(Verbs.CloneChildBuildConfig, HelpText = "Clones Build Config to the different root Build Config. Root Build Config will depend on cloned dependency Build Config. Cloned dependency Build Config will continue to have same dependencies.")]
    public class CloneChildBuildConfigOptions
    {
        [Option('b', "buildId", Required = true, HelpText = "Build Id to base cloned Build Config on.")]
        public string BuildId { get; set; }
     
        [Option('c', "cloneForRootBuildConfigId", Required = true, HelpText = "Root Build Config Id for which dependency is cloned.")]
        public string CloneForRootBuildConfigId { get; set; }

        [Option('s', "Simulate", Required = false, HelpText = "When set to true it doesn't make any changes to TeamCity. Only provides intended execution path.")]
        public bool Simulate { get; set; }

    }
}