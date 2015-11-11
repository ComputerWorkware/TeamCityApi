using System.Threading.Tasks;
using NSubstitute;
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.Xunit;
using TeamCityApi.Clients;
using TeamCityApi.Domain;
using TeamCityApi.Helpers;
using TeamCityApi.Locators;
using TeamCityApi.Tests.Helpers;
using TeamCityApi.Tests.Scenarios;
using TeamCityApi.UseCases;
using Xunit;
using Xunit.Extensions;

namespace TeamCityApi.Tests.UseCases
{
    public class CloneRootBuildConfigUseCaseTests
    {
        [Theory]
        [AutoNSubstituteData]
        public void Should_clone_root_build_config(
            int sourceBuildId, 
            string newNameSuffix, 
            ITeamCityClient client, 
            IBuildConfigXmlClient buildConfigXmlClient, 
            IFixture fixture,
            IVcsRootHelper vcsRootHelper)
        {
            var scenario = new SingleBuildScenario(fixture, client, sourceBuildId);
            
            var sut = new CloneRootBuildConfigUseCase(client, buildConfigXmlClient, vcsRootHelper);

            sut.Execute(sourceBuildId, newNameSuffix, false).Wait();

            var newBuildConfigName = scenario.BuildConfig.Name + Consts.SuffixSeparator + newNameSuffix;
            buildConfigXmlClient.Received(1)
                .CopyBuildConfiguration(scenario.BuildConfig.Id, scenario.Build.StartDate, Arg.Any<string>(), newBuildConfigName);
        }
    }
}