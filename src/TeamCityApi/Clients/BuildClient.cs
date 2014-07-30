﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TeamCityApi.Domain;
using TeamCityApi.Locators;

namespace TeamCityApi.Clients
{
    public interface IBuildClient
    {
        Task<Build> ById(string id);
        Task<List<BuildSummary>> ByBuildLocator(Action<BuildLocator> locatorConfig);
        Task<List<File>> GetFiles(long buildId);
        Task<Build> LastSuccessfulBuildFromConfig(string buildConfigId);
    }

    public class BuildClient : IBuildClient
    {
        private readonly IHttpClientWrapper _http;

        public BuildClient(IHttpClientWrapper http)
        {
            _http = http;
        }

        public async Task<Build> ById(string id)
        {
            string requestUri = string.Format("/app/rest/builds/id:{0}", id);

            var build = await _http.Get<Build>(requestUri);

            build.ArtifactsReference.Initialize(_http);

            return build;
        }

        public async Task<List<BuildSummary>> ByBuildLocator(Action<BuildLocator> locatorConfig)
        {
            var buildLocator = new BuildLocator();

            locatorConfig(buildLocator);

            string requestUri = string.Format("/app/rest/builds?locator={0}", buildLocator);

            var buildWrapper = await _http.Get<BuildWrapper>(requestUri);

            if (buildWrapper.Build.Count > 0)
            {
                foreach (var buildSummary in buildWrapper.Build)
                {
                    buildSummary.SetBuildClient(this);
                }

                return buildWrapper.Build;
            }

            return new List<BuildSummary>();
        }

        public async Task<List<File>> GetFiles(long buildId)
        {
            string requestUri = string.Format("/app/rest/builds/id:{0}/artifacts/children", buildId);

            List<File> files = await _http.Get<List<File>>(requestUri);

            files.ForEach(file => file.Initialize(_http));

            return files;
        }

        public async Task<Build> LastSuccessfulBuildFromConfig(string buildConfigId)
        {
            List<BuildSummary> buildSummaries = await ByBuildLocator(locator =>
                locator.WithBuildStatus(BuildStatus.Success)
                    .WithMaxResults(1)
                    .WithBuildConfiguration(typeLocator =>
                        typeLocator.WithId(buildConfigId)));

            BuildSummary buildSummary = buildSummaries.First();

            Build build = await buildSummary.GetDetails();

            return build;
        }
    }
}