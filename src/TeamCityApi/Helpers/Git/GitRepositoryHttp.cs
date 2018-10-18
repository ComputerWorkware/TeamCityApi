using System;
using System.Collections.Generic;
using System.Linq;
using LibGit2Sharp;
using TeamCityApi.Logging;

namespace TeamCityApi.Helpers.Git
{
    public class GitRepositoryHttp : GitRepository
    {
        private static readonly ILog Log = LogProvider.GetCurrentClassLogger();

        private readonly List<GitCredential> _credentials;

        public GitRepositoryHttp(string repositoryLocation, string tempClonePath, string sshKeyFolder, List<GitCredential> credentials) : base(repositoryLocation, tempClonePath, sshKeyFolder)
        {
            _credentials = credentials;
        }

        public override void Push(string branchName)
        {
            Log.Debug($"Pushing Branch {branchName} using HTTP Authentication");
            try
            {
                PushOptions options = new PushOptions()
                {
                    CredentialsProvider = (_url, _user, _cred) => LookupCredentials(_url, _user, _cred)
                };

                using (var repo = new Repository(TempClonePath))
                {
                    Branch branch = repo.Branches.FirstOrDefault(b => b.FriendlyName == branchName);
                    if (branch == null)
                    {
                        throw new Exception($"Local Branch: {branchName} cannot be found.");
                    }
                    repo.Network.Push(branch, options);
                }
            }
            catch (Exception exception)
            {
                Log.ErrorException($"Exception during Push of branch: {branchName}", exception);
                throw;
            }

            Log.Debug($"Branch {branchName} successfully pushed to remote.");
        }

        public bool Clone(CloneOptions options)
        {
            if (options==null)
                options = new CloneOptions();

            Log.Debug($"Clone repository with Http: {RepositoryLocation} into {TempClonePath}");
            options.CredentialsProvider = (_url, _user, _cred) => LookupCredentials(_url, _user, _cred);

            string clone = Repository.Clone(RepositoryLocation, TempClonePath, options);

            if (string.IsNullOrWhiteSpace(clone))
            {
                Log.Error($"Could not clone repository: {RepositoryLocation}");
            }
            else
            {
                Log.Debug($"Repository cloned successfully: {clone}");
            }

            return !string.IsNullOrWhiteSpace(clone);
        }

        public override bool Clone()
        {
            Log.Debug($"Clone repository with Http: {RepositoryLocation} into {TempClonePath}");
            var options = new CloneOptions
            {
                CredentialsProvider = (_url, _user, _cred) => LookupCredentials(_url, _user, _cred)
            };

            string clone = Repository.Clone(RepositoryLocation, TempClonePath, options);

            if (string.IsNullOrWhiteSpace(clone))
            {
                Log.Error($"Could not clone repository: {RepositoryLocation}");
            }
            else
            {
                Log.Debug($"Repository cloned successfully: {clone}");
            }

            return !string.IsNullOrWhiteSpace(clone);
        }

        private Credentials LookupCredentials(string url, string usernameFromUrl, SupportedCredentialTypes supportedCredentialTypes)
        {
            Log.Debug($"Lookup of Credentials: url: {url} usernameFromUrl: {usernameFromUrl}, supported Type: {supportedCredentialTypes}");
            string urlLowercased = url.ToLowerInvariant();
            var credential = _credentials.First();
            if (credential != null)
            {
                Log.Debug($"Credentials found: {credential.HostName}, {credential.UserName}:{credential.Password}");
                return new UsernamePasswordCredentials()
                {
                    Username = credential.UserName,
                    Password = credential.Password
                };
            }

            Log.Debug("No credentials found in lookup.");

            return null;

        }
    }
}