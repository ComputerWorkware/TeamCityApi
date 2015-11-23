using NSubstitute;
using Ploeh.AutoFixture;
using TeamCityApi.Clients;
using TeamCityApi.Helpers.Git;
using TeamCityApi.Tests.Helpers;
using TeamCityApi.Tests.Scenarios;
using TeamCityApi.UseCases;
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
            var scenario = new SingleBuildScenario(fixture, client, buildConfigXmlClient, sourceBuildId);

            var sut = new CloneRootBuildConfigUseCase(client, buildConfigXmlClient, vcsRootHelper);

            sut.Execute(sourceBuildId, newNameSuffix, false).Wait();

            scenario.BuildConfigXml.Received(1)
                .CopyBuildConfiguration(Arg.Any<string>(), Arg.Any<string>());
        }
    }
}