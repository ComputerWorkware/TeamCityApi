using TeamCityApi;
using TeamCityApi.UseCases;
using TeamCityConsole.Options;
using TeamCityConsole.Tests.Helpers;
using Xunit.Extensions;


namespace TeamCityConsole.Tests.Commands
{
    public class DeleteClonedBuildChainCommandTests
    {
         public class Execute
         {
             [Theory]
             [AutoNSubstituteData]
             public void Should_delete_build_chain(DeleteClonedBuildChainOptions clonedBuildChainOptions)
             {
                 var deleteClonedBuildChainUseCase = new DeleteClonedBuildChainUseCase(new TeamCityClient("teamcitytest:8080", "teamcity", "teamcity"));
                 clonedBuildChainOptions.BuildConfigId = "Installers_Sunlife_VitalObjectsSuite_TrunkKrisTest";
                 clonedBuildChainOptions.Simulate = true;
                deleteClonedBuildChainUseCase.Execute("Installers_Sunlife_VitalObjectsSuite_TrunkKrisTest").Wait();
             }
         }
       
    }
}