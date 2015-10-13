using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TeamCityConsole.Commands;
using TeamCityConsole.Options;
using TeamCityConsole.Tests.Helpers;
using Xunit;
using Xunit.Extensions;

namespace TeamCityConsole.Tests.Commands
{
    public class CloneBuildConfigCommandTests
    {
        public class Execute
        {
            [Theory]
            [AutoNSubstituteData]
            public void Should_clone_root_build_config(TestCloneBuildConfigCommand cloneBuildConfigCommand, CloneBuildConfigOptions cloneBuildConfigOptions)
            {
                cloneBuildConfigOptions.Mode = CloneBuildConfigOptions.CloneMode.Root;
                cloneBuildConfigOptions.NewNameSuffix = "Release Oct 13";

                cloneBuildConfigCommand.Execute(cloneBuildConfigOptions).Wait();

                //todo: assert that succeeded. For now just tests that no exceptions are thrown.
            }

            [Theory]
            [AutoNSubstituteData]
            public void Should_throw_when_suffix_not_provided_for_root_mode(TestCloneBuildConfigCommand cloneBuildConfigCommand, CloneBuildConfigOptions cloneBuildConfigOptions)
            {
                cloneBuildConfigOptions.Mode = CloneBuildConfigOptions.CloneMode.Root;
                cloneBuildConfigOptions.NewNameSuffix = "";

                var exception = Assert.Throws<AggregateException>(() => cloneBuildConfigCommand.Execute(cloneBuildConfigOptions).Wait());

                Assert.True(exception.InnerExceptions[0].Message.StartsWith("newNameSuffix should be provided in Root mode."));
            }

            [Theory]
            [AutoNSubstituteData]
            public void Should_throw_when_cloneForRootBuildConfigId_not_provided_for_child_mode(TestCloneBuildConfigCommand cloneBuildConfigCommand, CloneBuildConfigOptions cloneBuildConfigOptions)
            {
                cloneBuildConfigOptions.Mode = CloneBuildConfigOptions.CloneMode.Child;
                cloneBuildConfigOptions.CloneForRootBuildConfigId = "";

                var exception = Assert.Throws<AggregateException>(() => cloneBuildConfigCommand.Execute(cloneBuildConfigOptions).Wait());

                Assert.True(exception.InnerExceptions[0].Message.StartsWith("cloneForRootBuildConfigId should be provided in Child mode."));
            }

        }

        public class TestCloneBuildConfigCommand : CloneBuildConfigCommand
        {

        }
    }
}
