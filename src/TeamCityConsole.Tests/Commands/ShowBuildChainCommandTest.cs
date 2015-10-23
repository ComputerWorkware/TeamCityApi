using TeamCityApi;
using TeamCityApi.UseCases;
using TeamCityConsole.Options;
using TeamCityConsole.Tests.Helpers;
using Xunit.Extensions;

namespace TeamCityConsole.Tests.Commands
{
    public class ShowBuildChainCommandTest
    {

        public class Execute
        {
            [Theory]
            [AutoNSubstituteData]
            public void Should_delete_build_chain(ShowBuildChainOptions showBuildChainOptions)
            {
                var showBuildChainUseCase = new ShowBuildChainUseCase(new TeamCityClient("teamcitytest:8080", "teamcity", "teamcity"));
                showBuildChainOptions.BuildConfigId = "Installers_Sunlife_VitalObjectsSuite_TrunkKris";

                showBuildChainUseCase.Execute("Installers_Sunlife_VitalObjectsSuite_TrunkKris").Wait();
            }
        }
    }
}