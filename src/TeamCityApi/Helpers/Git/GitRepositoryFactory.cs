using System;
using System.Collections.Generic;
using System.IO;
using TeamCityApi.Domain;
using TeamCityApi.Logging;

namespace TeamCityApi.Helpers.Git
{
    public class Credential
    {
        public string UserName { get; set; }
        public string Password { get; set; }
        public string HostName { get; set; }
    }

    public class GitRepositoryFactory
    {
        private static readonly ILog Log = LogProvider.GetLogger(typeof(GitRepositoryFactory));

        private readonly List<Credential> _credentials;
        public string SSHKeyFolder { get; set; }

        public GitRepositoryFactory(List<Credential> credentials)
        {
            _credentials = credentials;
            SSHKeyFolder = SshKeyHelper.GetSSHKeyFolder();
        }


        public IGitRepository Clone(VcsCommit commitInfo)
        {
            string tempFolderPath = Path.GetTempPath();
            string guidTempPath = Guid.NewGuid().ToString().Replace("-", "");
            string temporaryClonePath = Path.Combine(tempFolderPath, guidTempPath);

            Log.Info(string.Format("Clone Repository: {0} into {1}",commitInfo,temporaryClonePath));

            GitRepository repository = new GitRepository(commitInfo, temporaryClonePath,SSHKeyFolder,_credentials);
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
