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
            var scenario = new ChainWithRootClonedScenario(fixture, client);
            var sut = new CloneChildBuildConfigUseCase(client, vcsRootHelper, buildConfigXmlClient);

            sut.Execute(scenario.ComponentA.BuildConfig.Id, scenario.SuiteCloned.BuildConfig.Id, simulate:false).Wait();
            
            client.BuildConfigs.Received(1).CopyBuildConfiguration(Arg.Any<string>(), Arg.Any<string>(), scenario.AppA.BuildConfig.Id, Arg.Any<bool>(), Arg.Any<bool>());
            client.BuildConfigs.Received(1).CopyBuildConfiguration(Arg.Any<string>(), Arg.Any<string>(), scenario.InstallerA.BuildConfig.Id, Arg.Any<bool>(), Arg.Any<bool>());
            client.BuildConfigs.DidNotReceive().CopyBuildConfiguration(Arg.Any<string>(), Arg.Any<string>(), scenario.SuiteCloned.BuildConfig.Id, Arg.Any<bool>(), Arg.Any<bool>());
            client.BuildConfigs.DidNotReceive().CopyBuildConfiguration(Arg.Any<string>(), Arg.Any<string>(), scenario.Suite.BuildConfig.Id, Arg.Any<bool>(), Arg.Any<bool>());
            client.BuildConfigs.Received(1).CopyBuildConfiguration(Arg.Any<string>(), Arg.Any<string>(), scenario.InstallerB.BuildConfig.Id, Arg.Any<bool>(), Arg.Any<bool>());
            client.BuildConfigs.Received(1).CopyBuildConfiguration(Arg.Any<string>(), Arg.Any<string>(), scenario.AppB.BuildConfig.Id, Arg.Any<bool>(), Arg.Any<bool>());
            client.BuildConfigs.Received(1).CopyBuildConfiguration(Arg.Any<string>(), Arg.Any<string>(), scenario.ComponentA.BuildConfig.Id, Arg.Any<bool>(), Arg.Any<bool>());
            client.BuildConfigs.Received(1).CopyBuildConfiguration(Arg.Any<string>(), Arg.Any<string>(), scenario.ComponentB.BuildConfig.Id, Arg.Any<bool>(), Arg.Any<bool>());
            client.BuildConfigs.DidNotReceive().CopyBuildConfiguration(Arg.Any<string>(), Arg.Any<string>(), scenario.ComponentC.BuildConfig.Id, Arg.Any<bool>(), Arg.Any<bool>());

            //todo: received call(s) to update artifact dependency
            //todo: received call(s) to create snapshot dependency



        }
    }
}