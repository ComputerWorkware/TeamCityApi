using TeamCityApi;
using TeamCityApi.UseCases;
using TeamCityConsole.Options;
using TeamCityConsole.Tests.Helpers;
using Xunit.Extensions;

namespace TeamCityConsole.Tests.Commands
{
    public class CompareBuildsCommandTest
    {
        public class Execute
        {
            [Theory]
            [AutoNSubstituteData]
            public void Should_compare_builds(CompareBuildsOptions compareBuildsOptions)
            {
                var compareBuildsUseCase = new CompareBuildsUseCase(new TeamCityClient("devciserver:8080", "ciserver", "ciserver"));
                compareBuildsUseCase.Execute("178416", "180701").Wait();

                //var compareBuildsUseCase = new CompareBuildsUseCase(new TeamCityClient("teamcitytest:8080", "teamcity", "teamcity"));
                //compareBuildsUseCase.Execute("298", "456").Wait();
            }
        }
    }
}