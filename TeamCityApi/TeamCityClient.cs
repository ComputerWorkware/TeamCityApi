using System;
using TeamCityApi.Clients;

namespace TeamCityApi
{
    public class TeamCityClient
    {
        private readonly IHttpClientWrapper _http;

        private readonly Lazy<IBuildClient> _buildClient;
        private readonly Lazy<IBuildConfigClient> _buildConfigClient;

        public IBuildClient Builds
        {
            get { return _buildClient.Value; }
        }

        public IBuildConfigClient BuildConfigs
        {
            get { return _buildConfigClient.Value; }
        }

        public TeamCityClient(string hostname, string username, string password) : this(new HttpClientWrapper(hostname, username, password))
        {
            
        }

        public TeamCityClient(IHttpClientWrapper http)
        {
            _http = http;
            _buildClient = new Lazy<IBuildClient>(() => new BuildClient(_http));
            _buildConfigClient = new Lazy<IBuildConfigClient>(() => new BuildConfigClient(_http));
        }
    }
}