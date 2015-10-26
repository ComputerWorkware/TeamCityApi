using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using LibGit2Sharp;
using TeamCityApi.Domain;
using TeamCityApi.Helpers;
using TeamCityApi.Logging;

namespace TeamCityApi.UseCases
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


        public GitRepository Clone(VcsCommit commitInfo)
        {
            string tempFolderPath = Path.GetTempPath();
            string guidTempPath = Guid.NewGuid().ToString().Replace("-", "");
            string temporaryClonePath = Path.Combine(tempFolderPath, guidTempPath);

            GitRepository repository = new GitRepository(commitInfo, temporaryClonePath,SSHKeyFolder,_credentials);
            if (repository.Clone())
                return repository;

            return null;
        }
    }

    public class GitRepository
    {
        private static readonly ILog Log = LogProvider.GetLogger(typeof(GitRepository));
        private readonly VcsCommit _commitInfo;
        private readonly string _sshKeyFolder;
        private readonly List<Credential> _credentials;

        public GitRepository(VcsCommit commitInfo, string temporaryClonePath, string sshKeyFolder, List<Credential> credentials)
        {
            _commitInfo = commitInfo;
            _sshKeyFolder = sshKeyFolder;
            _credentials = credentials;
            Location = temporaryClonePath;
        }

        public string Location { get; set; }

        public VcsCommit CommitInfo
        {
            get { return _commitInfo; }
        }

        public bool Push(string branchName)
        {
            Log.Info(string.Format("Push branch: {0}",branchName));
            switch (_commitInfo.AuthenticationType)
            {
                case GitAuthenticationType.Ssh:
                    return PushSSH(branchName);
                    break;
                case GitAuthenticationType.Http:
                    return PushHttp(branchName);
                    break;
                default:
                {
                    Log.Error(string.Format("Cannot push Branch:{0} , invalid Authentication Type",branchName));
                    return false;
                }
            }
        }

        public bool ArchiveCurrentBranch(string zipFileLocation)
        {
            Log.Info(string.Format("Archive Current Branch into zip: {0}",zipFileLocation));
            var startInfo = GetStartInfo();
            startInfo.WorkingDirectory = Location;
            startInfo.Arguments = string.Format(@"archive --format=zip -o ""{0}"" HEAD", zipFileLocation);

            var process = new Process { StartInfo = startInfo };
            process.Start();
            process.WaitForExit();

            LogCommandLine(startInfo, process);

            if (process.ExitCode == 0)
            {
                Log.Info("ArchiveCurrentBranch successful");
            }
            else
            {
                Log.Error(string.Format("Unable to ArchiveCurrentBranch , Exit code: {0}", process.ExitCode));
            }

            return (process.ExitCode == 0);
            
        }

        private void LogCommandLine(ProcessStartInfo startInfo, Process process)
        {
            Log.Debug(string.Format("** Start - Command Line Action - {0} {1}",startInfo.FileName,startInfo.Arguments ));
            Log.Debug("  ** Standard Error **");
            string lineVal = process.StandardError.ReadLine();
            while (lineVal != null)
            {
                Log.Info(lineVal);
                lineVal = process.StandardError.ReadLine();
            }
            Log.Debug("  ** Standard Output **");
             lineVal = process.StandardOutput.ReadLine();
            while (lineVal != null)
            {
                Log.Info(lineVal);
                lineVal = process.StandardOutput.ReadLine();
            }
            Log.Debug(string.Format("  ** Exit Code: {0}", process.ExitCode));
            Log.Debug(string.Format("** End - Command Line Action - {0} {1}", startInfo.FileName, startInfo.Arguments));
        }

        public bool ArchiveTreeIsh(string zipFileLocation,string treeIsh)
        {
            Log.Info(String.Format("Archive Git Tree: {0} into Zip: {1}",treeIsh,zipFileLocation));
            var startInfo = GetStartInfo();
            startInfo.WorkingDirectory = Location;
            startInfo.Arguments = string.Format(@"archive --format=zip -o ""{0}"" {1}", zipFileLocation,treeIsh);

            var process = new Process { StartInfo = startInfo };
            process.Start();
            process.WaitForExit();

            LogCommandLine(startInfo, process);

            if (process.ExitCode == 0)
            {
                Log.Info("ArchiveTreeIsh successful");
            }
            else
            {
                Log.Error(string.Format("Unable to ArchiveTreeIsh , Exit code: {0}", process.ExitCode));
            }


            return (process.ExitCode == 0);

        }

        public bool CheckoutBranch(string branchName)
        {
            Log.Info(string.Format("Checkout Branch: {0}",branchName));
            using (var repo = new Repository(Location))
            {
                List<Branch> branches = repo.Branches.ToList();

                Branch branch = repo.Branches.FirstOrDefault(b => b.Name == branchName && !b.IsRemote);
                if (branch == null)
                {
                    Log.Debug(string.Format("Local branch {0} cannot be found, looking for remote branch.",branchName));
                    string originBranch = string.Format("origin/{0}", branchName);
                    var trackingBranch = repo.Branches.FirstOrDefault(b => b.Name == originBranch && b.IsRemote);
                    if (trackingBranch != null)
                    {
                        Log.Debug(string.Format("Remote branch: {0} found ",originBranch));
                        branch = repo.CreateBranch(branchName, trackingBranch.Tip);
                        branch = repo.Branches.Update(branch, b => b.TrackedBranch = trackingBranch.CanonicalName);
                        repo.Checkout(branch);
                        return true;
                    }
                    else
                    {
                        Log.Error(string.Format("Remote branch: {0} cannot be found, cannot create branch: {1} ", originBranch,branchName));
                    }
                }
                else
                {
                    repo.Checkout(branch);
                }
            }
            return true;
            
        }

        private bool PushSSH(string branchName)
        {
            Log.Debug(string.Format("Pushing Branch {0} using SSH Authentication",branchName));
            var startInfo = GetStartInfo();
            startInfo.WorkingDirectory = Location;
            startInfo.Arguments = string.Format("push -u origin {0}", branchName);

            var process = new Process { StartInfo = startInfo };
            process.Start();
            process.WaitForExit();

            LogCommandLine(startInfo, process);

            if (process.ExitCode == 0)
            {
                Log.Info(string.Format("Branch {0} successfully pushed to remote.", branchName));
            }
            else
            {
                Log.Error(string.Format("Unable to push branch: {0} , Exit code: {1}",branchName,process.ExitCode));
            }

            return (process.ExitCode == 0);
        }

        private bool PushHttp(string branchName)
        {
            Log.Debug(string.Format("Pushing Branch {0} using HTTP Authentication", branchName));
            try
            {
                PushOptions options = new PushOptions()
                {
                    CredentialsProvider = (_url, _user, _cred) => LookupCredentials(_url, _user, _cred)
                };

                using (var repo = new Repository(Location))
                {
                    Branch branch =
                        repo.Branches.FirstOrDefault(b => b.Name == branchName);
                    if (branch == null)
                    {
                        Log.Error(string.Format("Local Branch: {0} cannot be found.", branchName));
                        return false;
                    }
                    repo.Network.Push(branch, options);
                }
            }
            catch (Exception exception)
            {
                Log.ErrorException(string.Format("Exception during Push of branch: {0}",branchName),exception);
                return false;
            }

            Log.Info(string.Format("Branch {0} successfully pushed to remote.",branchName));
            return true;
        }

        public void DeleteFolder()
        {
            Log.Info(string.Format("Recursively Delete Folder: {0}",Location));

            var directoryInfo = new DirectoryInfo(Location);
            if (directoryInfo.Exists)
            {
                foreach (FileInfo fileInfo in directoryInfo.EnumerateFiles("*.*", SearchOption.AllDirectories).Where(fi=>fi.IsReadOnly))
                {
                    fileInfo.IsReadOnly = false;
                }
                directoryInfo.Delete(true);
            }
        }

        public bool CheckBranchExist(string branchName)
        {
            Log.Info(string.Format("Checking for Branch Existence: {0}",branchName));
            using (var repo = new Repository(Location))
            {
                string originBranch = String.Format("origin/{0}",branchName);
                var firstOrDefault = repo.Branches.FirstOrDefault(b => b.IsRemote && b.Name == originBranch);
                if (firstOrDefault==null)
                    Log.Info(string.Format("Branch {0} does not exist",branchName));
                else
                {
                    Log.Info(string.Format("Branch {0} exists", branchName));
                }
                return firstOrDefault != null;
            }
        }

        public bool AddBranch(string branchName,string commitSha)
        {
            Log.Info(string.Format("Add Local Branch {0} at specific commit: {1}", branchName, commitSha));
            if (CheckBranchExist(branchName))
            {
                Log.Info(string.Format("Local Branch already exists: {0}",branchName));
                return false;
            }

            using (var repo = new Repository(Location))
            {
                Remote remote = repo.Network.Remotes["origin"];

                Branch branch = repo.Branches.Add(branchName,commitSha);
                if (branch != null)
                {
                    repo.Branches.Update(branch,
                        b => b.Remote = remote.Name,
                        b => b.UpstreamBranch = branch.CanonicalName);
                    return true;
                }
            }
            Log.Error(String.Format("Failed to add local branch: {0}",branchName));
            return false;
        }

        public bool Clone()
        {
            Log.Info(string.Format("Clone Repository {0} into folder: {1}",_commitInfo.RespositoryLocation,Location));
            switch (CommitInfo.AuthenticationType)
            {
                case GitAuthenticationType.Ssh:
                    return CloneSSH();
                    break;
                case GitAuthenticationType.Http:
                    return CloneHttp();
                    break;
                default:
                    return false;
            }
        }

        private ProcessStartInfo GetStartInfo()
        {
            var startInfo = new ProcessStartInfo(@"git.exe");
            startInfo.EnvironmentVariables["HOME"] = _sshKeyFolder;
            startInfo.EnvironmentVariables["USERPROFILE"] = _sshKeyFolder;
            startInfo.EnvironmentVariables["HOMEPATH"] = _sshKeyFolder;
            startInfo.UseShellExecute = false;
            startInfo.WorkingDirectory = Path.GetTempPath(); 
            startInfo.RedirectStandardInput = true;
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;
            return startInfo;
        }

        private bool CloneHttp()
        {
            Log.Debug(string.Format("Clone repository with Http: {0} into {1}",_commitInfo.RespositoryLocation,Location));
            var options = new CloneOptions
            {
                CredentialsProvider = (_url, _user, _cred) => LookupCredentials(_url, _user, _cred)
            };

            string clone = Repository.Clone(_commitInfo.RespositoryLocation, Location, options);

            if (string.IsNullOrWhiteSpace(clone))
            {
                Log.Error(string.Format("Could not clone repository: {0}",_commitInfo.RespositoryLocation));    
            }
            else
            {
                Log.Debug(string.Format("Repository cloned successfully: {0}", clone));    
            }

            return !string.IsNullOrWhiteSpace(clone);
        }

        private Credentials LookupCredentials(string url, string usernameFromUrl, SupportedCredentialTypes supportedCredentialTypes)
        {
            Log.Debug(string.Format("Lookup of Credentials: url: {0} usernameFromUrl: {1}, supported Type: {2}",url,usernameFromUrl,supportedCredentialTypes));
            string urlLowercased = url.ToLowerInvariant();
            var credential = _credentials.FirstOrDefault(x=>urlLowercased.Contains(x.HostName.ToLowerInvariant()));
            if (credential != null)
            {
                Log.Debug(String.Format("Credentials found: {0}, {1}:{2}",credential.HostName,credential.UserName,credential.Password));
                return new UsernamePasswordCredentials()
                {
                    Username = credential.UserName,
                    Password = credential.Password
                };
            }

            Log.Debug("No credentials found in lookup.");

            return null;

        }

        private bool CloneSSH()
        {
            Log.Debug(string.Format("Clone repository with SSH: {0} into {1}", _commitInfo.RespositoryLocation, Location));
            var startInfo = GetStartInfo();
            startInfo.Arguments = string.Format("clone {0} {1} --verbose --progress", _commitInfo.RespositoryLocation,
                Location);

            var process = new Process { StartInfo = startInfo };
            process.Start();
            process.WaitForExit();

            LogCommandLine(startInfo, process);

            if (process.ExitCode == 0)
            {
                Log.Info(string.Format("Clone {0} with SSH was successful", _commitInfo.RespositoryLocation));
            }
            else
            {
                Log.Error(string.Format("Failed to clone repository: {0} , Exit code: {1}", _commitInfo.RespositoryLocation, process.ExitCode));
            }

            return (process.ExitCode == 0);
        }
    }
}
