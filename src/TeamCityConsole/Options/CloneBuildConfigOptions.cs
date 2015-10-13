using System;
using System.Collections.Generic;
using CommandLine;

namespace TeamCityConsole.Options
{
    [Verb(Verbs.CloneBuildConfig, HelpText = "Clones either Root or Child Build Config.               " +
                                             "--------------------------------------------------------" +
                                             "In Root mode it creates a shallow clone of build config " +
                                             "and configures its dependencies to point the same build " +
                                             "configurations.                                         " +
                                             "Dependency versions are frozen.                         " +
                                             "newNameSuffix param is required in this mode.           " +
                                             "--------------------------------------------------------" +
                                             "In Child mode it clones dependency build config to a    " +
                                             "different root build configuration. New root build      " +
                                             "configuration will start to depend on cloned dependency " +
                                             "build configuration. Cloned dependency build config     " +
                                             "will continue depend on the same build configs.         ")]
    public class CloneBuildConfigOptions
    {
        public enum CloneMode
        {
            Root,
            Child
        }

        [Option('b', "buildId", Required = true, HelpText = "Build Id to base cloned build config on.")]
        public string BuildId { get; set; }

        [Option('m', "mode", Required = true, HelpText = "'Root' mode is the first step. It will clone build config and freeze dependencies versions. 'Child' clones one of the dependencies for the new root.")]
        public CloneMode Mode { get; set; }

        [Option('n', "newNameSuffix", Required = false, HelpText = "A suffix to append to cloned build configuration name. Can be a release date, feature name etc. Used only in Root mode.")]
        public string NewNameSuffix { get; set; }

        [Option('c', "cloneForRootBuildConfigId", Required = false, HelpText = "Root Build Config Id for which dependency is cloned. Used only in Child mode.")]
        public string CloneForRootBuildConfigId { get; set; }

    }
}