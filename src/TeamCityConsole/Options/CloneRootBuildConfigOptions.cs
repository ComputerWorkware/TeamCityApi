using System;
using System.Collections.Generic;
using CommandLine;

namespace TeamCityConsole.Options
{
    [Verb(Verbs.CloneRootBuildConfig, HelpText = "Creates a shallow clone of Build Config and freezes artifact dependencies.")]
    public class CloneRootBuildConfigOptions
    {
        [Option('b', "buildId", Required = true, HelpText = "Build Id from which to create a clone. Parameters and dependencies which were used in provided Build will be set to the cloned Build Config.")]
        public string BuildId { get; set; }
        
        [Option('n', "newNameSuffix", Required = true, HelpText = "A suffix to append to the cloned Build Config name. Can be a release date, feature name etc.")]
        public string NewNameSuffix { get; set; }
    }
}