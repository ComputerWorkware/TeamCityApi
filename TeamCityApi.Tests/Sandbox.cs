using System;
using System.Collections.Generic;
using TeamCityApi.Clients;
using TeamCityApi.Domain;
using TeamCityApi.Locators;
using Xunit;

namespace TeamCityApi.Tests
{
    public class Sandbox
    {
        [Fact]
        public void Build_ByLocator()
        {
            var buildClient = CreateBuildClient();

            List<BuildSummary> buildSummaries = buildClient.ByBuildLocator(x => x.SinceDate(new DateTime(2014, 05, 11))).Result;
        }

        [Fact]
        public void Build_ById()
        {
            var buildClient = CreateBuildClient();

            Build build = buildClient.ById(45).Result;
        }


        [Fact]
        public void GetFiles()
        {
            BuildClient buildClient = CreateBuildClient();

            List<File> result = buildClient.GetFiles(46).Result;
        }

        public class BuildConfig
        {
            [Fact]
            public void ById()
            {
                var client = CreateBuildConfigClient();
                var buildConfig = client.GetByConfigurationId("Installers_29_Xu_297dev").Result;
            }
        }

        public class Project
        {
            [Fact]
            public void ById()
            {
                var client = CreateProjectClient();
                var project = client.GetById("id29_BIFLoader").Result;
            }
        }

        public class Change
        {
            [Fact]
            public void GetAll()
            {
                var client = CreateChnageClient();
                var changes = client.GetAll().Result;
            }

            [Fact]
            public void GetById()
            {
                var client = CreateChnageClient();
                var change = client.GetById("4").Result;
            }
        }

        private static ChangeClient CreateChnageClient()
        {
            var http = CreateHttpClientWrapper();
            var client = new ChangeClient(http);
            return client;
        }

        private static ProjectClient CreateProjectClient()
        {
            var http = CreateHttpClientWrapper();
            var projectClient = new ProjectClient(http);
            return projectClient;
        }

        private static BuildConfigClient CreateBuildConfigClient()
        {
            var http = CreateHttpClientWrapper();
            var buildConfigClient = new BuildConfigClient(http);
            return buildConfigClient;
        }

        private static HttpClientWrapper CreateHttpClientWrapper()
        {
            var http = new HttpClientWrapper("localhost:8090", "administrator", "admin");
            return http;
        }

        private static BuildClient CreateBuildClient()
        {
            var http = new HttpClientWrapper("localhost:8090", "administrator", "admin");
            var buildClient = new BuildClient(http);
            return buildClient;
        }
    }
}