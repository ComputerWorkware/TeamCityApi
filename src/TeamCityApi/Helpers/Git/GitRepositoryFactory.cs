using System;
using System.Collections.Generic;
using System.IO;
using TeamCityApi.Domain;
using TeamCityApi.Logging;

namespace TeamCityApi.Helpers.Git
{
    public class GitCredential
    {
        public string UserName { get; set; }
        public string Password { get; set; }
        public string HostName { get; set; }
    }

    public interface IGitRepositoryFactory
    {
        IGitRepository Clone(VcsCommit commitInfo);
        IGitRepository Clone(GitAuthenticationType authenticationType, string repositoryLocation);
    }

    public class GitRepositoryFactory : IGitRepositoryFactory
    {
        private static readonly ILog Log = LogProvider.GetLogger(typeof(GitRepositoryFactory));

        private readonly List<GitCredential> _credentials;
        private string SshKeyFolder { get; set; }

        public GitRepositoryFactory(List<GitCredential> credentials)
        {
            _credentials = credentials;
            SshKeyFolder = SshKeyHelper.GetSSHKeyFolder();
        }

        public IGitRepository Clone(VcsCommit commitInfo)
        {
            return Clone(commitInfo.AuthenticationType, commitInfo.RepositoryLocation);
        }

        public IGitRepository Clone(GitAuthenticationType authenticationType, string repositoryLocation)
        {
            string tempFolderPath = "C:\\tmp";

            string temporaryClonePath = "";

            for (int i = 0; i < 100; i++)
            {
                string guidTempPath = RandomStringGenerator.GetSingleCase(8);
                temporaryClonePath = Path.Combine(tempFolderPath, guidTempPath);
                if (Directory.Exists(temporaryClonePath) == false)
                {
                    Directory.CreateDirectory(temporaryClonePath);
                    break;
                }
            }

            Log.Info($"Clone Repository: {repositoryLocation} into {temporaryClonePath}");

            GitRepository repository;

            switch (authenticationType)
            {
                case GitAuthenticationType.Ssh:
                    repository = new GitRepositorySsh(repositoryLocation, temporaryClonePath, SshKeyFolder);
                    break;
                case GitAuthenticationType.Http:
                    repository = new GitRepositoryHttp(repositoryLocation, temporaryClonePath, SshKeyFolder, _credentials);
                    break;
                default:
                    throw new Exception($"Non supported authentication type {authenticationType}");
            }

            if (repository.Clone())
            {
                Log.Info("Repository successfully cloned.");
                return repository;
            }

            Log.Error("Failed to clone repository");

            return null;
        }
    }
}
