using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TeamCityApi.Domain;
using TeamCityApi.Locators;

namespace TeamCityApi.Clients
{
    public interface IBuildClient
    {
        Task<Build> ById(long id);
        Task<List<BuildSummary>> ByBuildLocator(Action<BuildLocator> locator);
        Task<List<File>> GetFiles(long buildId);
    }

    public class BuildClient : IBuildClient
    {
        private readonly IHttpClientWrapper _http;

        public BuildClient(IHttpClientWrapper http)
        {
            _http = http;
        }

        public async Task<Build> ById(long id)
        {
            string requestUri = string.Format("/app/rest/builds/id:{0}", id);

            var build = await _http.Get<Build>(requestUri);

            return build;
        }

        public async Task<List<BuildSummary>> ByBuildLocator(Action<BuildLocator> locator)
        {
            var buildLocator = new BuildLocator();

            locator(buildLocator);

            string requestUri = string.Format("/app/rest/builds?locator={0}", locator);

            var buildWrapper = await _http.Get<BuildWrapper>(requestUri);

            if (buildWrapper.Build.Count > 0)
            {
                return buildWrapper.Build;
            }

            return new List<BuildSummary>();
        }

        public async Task<List<File>> GetFiles(long buildId)
        {
            string requestUri = string.Format("/app/rest/builds/id:{0}/artifacts/children", buildId);

            List<File> files = await _http.Get<List<File>>(requestUri);

            return files;
        }
    }
}