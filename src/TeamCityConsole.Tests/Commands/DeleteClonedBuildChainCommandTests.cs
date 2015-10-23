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