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
        public string GitLabToken { get; set; }
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
            if (string.IsNullOrWhiteSpace(GitLabSettings.GitLabToken))
            {
                throw new InvalidOperationException("GitLab access token is not configured. Set the 'gitlabtoken' app setting in TeamCityConsole.exe.config.");
            }

            return GitLabClient.Connect(GitLabSettings.GitLabUri, GitLabSettings.GitLabToken, NGitLab.Impl.Api.ApiVersion.V4);
        }
    }
}
