using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TeamCityApi;
using TeamCityApi.UseCases;
using TeamCityConsole.Commands;
using TeamCityConsole.Options;
using TeamCityConsole.Tests.Helpers;
using Xunit;
using Xunit.Extensions;

namespace TeamCityConsole.Tests.Commands
{
    public class CloneRootBuildConfigCommandTests
    {
        public class Execute
        {
            [Theory]
            [AutoNSubstituteData]
            public void Should_clone_root_build_config(TestCloneRootBuildConfigCommand cloneBuildConfigCommand, CloneRootBuildConfigOptions cloneRootBuildConfigCommand)
            {
                cloneRootBuildConfigCommand.BuildId = "268";
                cloneRootBuildConfigCommand.NewNameSuffix = "Release Oct 13";

                cloneBuildConfigCommand.Execute(cloneRootBuildConfigCommand).Wait();

                //todo: assert that succeeded. For now just tests that no exceptions are thrown.
            }
        }

        public class TestCloneRootBuildConfigCommand : CloneRootBuildConfigCommand
        {
            public TestCloneRootBuildConfigCommand(CloneRootBuildConfigUseCase cloneRootBuildConfigUseCase) : base(cloneRootBuildConfigUseCase)
            {

            }
        }
    }
}
