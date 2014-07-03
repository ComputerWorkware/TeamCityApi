using System.Collections.Generic;
using System.Threading.Tasks;
using TeamCityApi.Domain;

namespace TeamCityApi.Clients
{
    public interface IBuildConfigClient
    {
        Task<List<BuildConfigSummary>> GetAll();
        Task<BuildConfig> GetByConfigurationId(string buildConfigId);
    }

    public class BuildConfigClient : IBuildConfigClient
    {
        private readonly IHttpClientWrapper _http;

        public BuildConfigClient(IHttpClientWrapper http)
        {
            _http = http;
        }

        public async Task<List<BuildConfigSummary>> GetAll()
        {
            string requestUri = string.Format("/app/rest/buildTypes");

            List<BuildConfigSummary> buildConfigs = await _http.Get<List<BuildConfigSummary>>(requestUri);

            return buildConfigs;
        }

        public async Task<BuildConfig> GetByConfigurationId(string buildConfigId)
        {
            string requestUri = string.Format("/app/rest/buildTypes/id:{0}", buildConfigId);

            BuildConfig buildConfig = await _http.Get<BuildConfig>(requestUri);

            return buildConfig;
        }
    }
}