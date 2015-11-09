﻿using System.Threading.Tasks;
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
    public class CloneRootBuildConfigUseCaseTests
    {
        [Theory]
        [AutoNSubstituteData]
        public void Should_clone_root_build_config(
            int sourceBuildId, 
            string newNameSuffix, 
            ITeamCityClient client, 
            IFixture fixture,
            IVcsRootHelper vcsRootHelper)
        {
            var scenario = new SingleBuildScenario(fixture, client, sourceBuildId);
            
            var sut = new CloneRootBuildConfigUseCase(client,vcsRootHelper);

            sut.Execute(sourceBuildId, newNameSuffix, false).Wait();

            var newBuildConfigName = scenario.BuildConfig.Name + Consts.SuffixSeparator + newNameSuffix;
            client.BuildConfigs.Received(1)
                .CopyBuildConfiguration(scenario.Project.Id, newBuildConfigName, scenario.BuildConfig.Id, Arg.Any<bool>(), Arg.Any<bool>());
        }
    }
}