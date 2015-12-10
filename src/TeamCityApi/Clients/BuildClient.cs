using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TeamCityApi.Domain;
using TeamCityApi.Locators;

namespace TeamCityApi.Clients
{
    public interface IBuildClient
    {
        Task<Build> ById(long id);
        Task<Build> ByNumber(string number, string buildTypeId);
        Task<List<BuildSummary>> ByBuildLocator(Action<BuildLocator> locatorConfig);
        Task<List<File>> GetFiles(long buildId);
        Task<Build> LastSuccessfulBuildFromConfig(string buildConfigId, string tag = null);
        Task<Properties> GetResultingProperties(long id);
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

            build.ArtifactsReference.Initialize(_http);

            return build;
        }

        public async Task<Build> ByNumber(string number, string buildTypeId)
        {
            var buildSummaries = await ByBuildLocator(l => l.WithNumber(number, buildTypeId));

            return await ById(buildSummaries.First().Id);
        }

        public async Task<List<BuildSummary>> ByBuildLocator(Action<BuildLocator> locatorConfig)
        {
            var buildLocator = new BuildLocator();

            locatorConfig(buildLocator);

            string requestUri = string.Format("/app/rest/builds?locator={0}", buildLocator);

            var buildWrapper = await _http.Get<BuildWrapper>(requestUri);

            if (buildWrapper == null || buildWrapper.Build == null || buildWrapper.Build.Count == 0)
            {
                throw new Exception(string.Format("Could not get build from TeamCity by locator: \"{0}\"", buildLocator));
            }

            foreach (var buildSummary in buildWrapper.Build)
            {
                buildSummary.SetBuildClient(this);
            }

            return buildWrapper.Build;
        }

        public async Task<List<File>> GetFiles(long buildId)
        {
            string requestUri = string.Format("/app/rest/builds/id:{0}/artifacts/children", buildId);

            List<File> files = await _http.Get<List<File>>(requestUri);

            files.ForEach(file => file.Initialize(_http));

            return files;
        }

        public async Task<Build> LastSuccessfulBuildFromConfig(string buildConfigId, string tag)
        {
            List<BuildSummary> buildSummaries = await ByBuildLocator(locator =>
            {
                locator.WithBuildStatus(BuildStatus.Success)
                    .WithMaxResults(1)
                    .WithBuildConfiguration(typeLocator =>
                        typeLocator.WithId(buildConfigId));

                if (!String.IsNullOrEmpty(tag))
                {
                    locator.WithTags(tag);
                }
            });

            BuildSummary buildSummary = buildSummaries.First();

            Build build = await buildSummary.GetDetails();

            return build;
        }

        public async Task<Properties> GetResultingProperties(long id)
        {
            return await _http.Get<Properties>($"/app/rest/builds/id:{id}/resulting-properties");
        }
    }
}