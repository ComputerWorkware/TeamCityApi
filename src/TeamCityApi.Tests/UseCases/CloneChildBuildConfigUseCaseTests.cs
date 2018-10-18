using NSubstitute;
using Ploeh.AutoFixture;
using TeamCityApi.Clients;
using TeamCityApi.Helpers;
using TeamCityApi.Tests.Helpers;
using TeamCityApi.Tests.Scenarios;
using TeamCityApi.UseCases;
using Xunit.Extensions;

namespace TeamCityApi.Tests.UseCases
{
    public class CloneChildBuildConfigUseCaseTests
    {
        [Theory]
        [AutoNSubstituteData]
        public void Should_clone_child_build_config(
            ITeamCityClient client,
            IVcsRootHelper vcsRootHelper,
            IBuildConfigXmlClient buildConfigXmlClient,
            IFixture fixture)
        {
            var scenario = new ChainWithRootClonedScenario(fixture, client, buildConfigXmlClient);
            var sut = new CloneChildBuildConfigUseCase(client, vcsRootHelper, buildConfigXmlClient);

            sut.Execute(scenario.ComponentA.BuildConfig.Id, scenario.SuiteCloned.BuildConfig.Id, simulate:false).Wait();

            scenario.AppA.BuildConfigXml.Received(1).CopyBuildConfiguration(Arg.Any<string>(), Arg.Any<string>());
            scenario.InstallerA.BuildConfigXml.Received(1).CopyBuildConfiguration(Arg.Any<string>(), Arg.Any<string>());
            scenario.SuiteCloned.BuildConfigXml.DidNotReceive().CopyBuildConfiguration(Arg.Any<string>(), Arg.Any<string>());
            scenario.Suite.BuildConfigXml.DidNotReceive().CopyBuildConfiguration(Arg.Any<string>(), Arg.Any<string>());
            scenario.InstallerB.BuildConfigXml.Received(1).CopyBuildConfiguration(Arg.Any<string>(), Arg.Any<string>());
            scenario.AppB.BuildConfigXml.Received(1).CopyBuildConfiguration(Arg.Any<string>(), Arg.Any<string>());
            scenario.ComponentA.BuildConfigXml.Received(1).CopyBuildConfiguration(Arg.Any<string>(), Arg.Any<string>());
            scenario.ComponentB.BuildConfigXml.Received(1).CopyBuildConfiguration(Arg.Any<string>(), Arg.Any<string>());
            scenario.ComponentC.BuildConfigXml.DidNotReceive().CopyBuildConfiguration(Arg.Any<string>(), Arg.Any<string>());

            //todo: received call(s) to update artifact dependency
            //todo: received call(s) to create snapshot dependency
        }
    }
}