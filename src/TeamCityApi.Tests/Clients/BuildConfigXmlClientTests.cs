using System.Threading.Tasks;
using NSubstitute;
using TeamCityApi.Clients;
using TeamCityApi.Domain;
using TeamCityApi.Helpers.Git;
using TeamCityApi.Tests.Helpers;
using Xunit;
using Xunit.Extensions;

namespace TeamCityApi.Tests.Clients
{
    public class BuildConfigXmlClientTests
    {
        [Theory]
        [AutoNSubstituteData]
        public void Should_clone_settings_repo(ITeamCityClient teamCityClient, IGitRepositoryFactory gitRepositoryFactory, Project project, string repoPath)
        {
            project.Properties.Property.Add(new Property(ParameterName.VersionedSettingGitRepo, repoPath));
            teamCityClient.Projects.GetById("RestHelper").Returns(Task.FromResult(project));

            var sut = new BuildConfigXmlClient(teamCityClient, gitRepositoryFactory);

            gitRepositoryFactory.Received(1).Clone(GitAuthenticationType.Http, repoPath);
        }
    }
}