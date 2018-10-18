using System;
using System.Collections.Generic;
using CommandLine;

namespace TeamCityConsole.Options
{
    [Verb(Verbs.DeepCloneBuildConfig, HelpText = "Creates a deep clone of Build Config, cloning all dependent build configurations.")]
    public class DeepCloneBuildConfigOptions
    {
        [Option('b', "buildId", Required = true, HelpText = "Build Id from which to create a clone. Parameters and dependencies which were used in provided Build will be set to the cloned Build Config.")]
        public long BuildId { get; set; }
        
        [Option('n', "newNameSuffix", Required = true, HelpText = "A suffix to append to the cloned Build Config name. Can be a release date, feature name etc.")]
        public string NewNameSuffix { get; set; }

        [Option('s', "Simulate", Required = false, HelpText = "When set to true it doesn't make any changes to TeamCity. Only provides intended execution path.")]
        public bool Simulate { get; set; }
    }
}