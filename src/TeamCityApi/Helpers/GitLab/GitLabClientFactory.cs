using System;
using System.Collections.Generic;
using System.IO;
using NGitLab;
using TeamCityApi.Domain;
using TeamCityApi.Logging;

namespace TeamCityApi.Helpers.Git
{

    public class GitLabSettings
    {
        public string GitLabUri { get; set; }
        public string GitLabUsername { get; set; }
        public string GitLabPassword { get; set; }
    }

    public interface IGitLabClientFactory
    {
        GitLabClient GetGitLabClient();
    }

    public class GitLabClientFactory : IGitLabClientFactory
    {
        private static readonly ILog Log = LogProvider.GetLogger(typeof(GitLabClientFactory));
        private GitLabSettings GitLabSettings { get; set; }

        public GitLabClientFactory(GitLabSettings gitLabSettings)
        {
            GitLabSettings = gitLabSettings;
        }

        public GitLabClient GetGitLabClient()
        {
            var client =  GitLabClient.Connect(GitLabSettings.GitLabUri, GitLabSettings.GitLabUsername, GitLabSettings.GitLabPassword);

            return client;
        }
    }
}
