using System;
using TeamCityApi.Clients;

namespace TeamCityApi
{
    public interface ITeamCityClient
    {
        IBuildClient Builds { get; }
        IBuildConfigClient BuildConfigs { get; }
        IProjectClient Projects { get; }
    }

    public class TeamCityClient : ITeamCityClient
    {
        private readonly IHttpClientWrapper _http;

        private readonly Lazy<IBuildClient> _buildClient;
        private readonly Lazy<IBuildConfigClient> _buildConfigClient;
        private readonly Lazy<IProjectClient> _projectsClient;

        public IBuildClient Builds
        {
            get { return _buildClient.Value; }
        }

        public IBuildConfigClient BuildConfigs
        {
            get { return _buildConfigClient.Value; }
        }

        public IProjectClient Projects
        {
            get { return _projectsClient.Value; }
        }

        public TeamCityClient(string hostname, string username, string password) : this(new HttpClientWrapper(hostname, username, password))
        {
            
        }

        public TeamCityClient(IHttpClientWrapper http)
        {
            _http = http;
            _buildClient = new Lazy<IBuildClient>(() => new BuildClient(_http));
            _buildConfigClient = new Lazy<IBuildConfigClient>(() => new BuildConfigClient(_http));
            _buildConfigClient = new Lazy<IBuildConfigClient>(() => new BuildConfigClient(_http));
            _projectsClient = new Lazy<IProjectClient>(() => new ProjectClient(_http));
        }
    }
}