using System.Threading.Tasks;
using NSubstitute;
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.Xunit;
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
    public class CloneChildBuildConfigUseCaseTests
    {
        [Theory]
        [AutoNSubstituteData]
        public void Should_clone_child_build_config(
            ITeamCityClient client,
            IVcsRootHelper vcsRootHelper,
            IGitRepository gitRepository, 
            IFixture fixture)
        {
            vcsRootHelper.CloneAndBranch(Arg.Any<long>(), Arg.Any<string>()).Returns(Task.FromResult(gitRepository));
            vcsRootHelper.PushAndDeleteLocalFolder(gitRepository, Arg.Any<string>()).Returns(true);

            var scenario = new ChainWithRootClonedScenario(fixture, client);
            var sut = new CloneChildBuildConfigUseCase(client, vcsRootHelper);

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