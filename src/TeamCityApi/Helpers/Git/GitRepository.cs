using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using LibGit2Sharp;
using TeamCityApi.Domain;
using TeamCityApi.Logging;

namespace TeamCityApi.Helpers.Git
{
    public interface IGitRepository
    {
        string TempClonePath { get; }
        void Push(string branchName);
        bool ArchiveCurrentBranch(string zipFileLocation);
        bool ArchiveTreeIsh(string zipFileLocation,string treeIsh);
        Branch CheckoutBranch(string branchName);
        Branch CheckoutMostRecentCommitBefore(string branchName, DateTime beforeDateTime);
        void DeleteFolder();
        bool CheckBranchExist(string branchName);
        bool AddBranch(string branchName,string commitSha);
        bool Clone();
        void StageAndCommit(IEnumerable<string> fileToStage, string message);
    }

    public abstract class GitRepository : IGitRepository
    {
        private static readonly ILog Log = LogProvider.GetLogger(typeof(GitRepository));
        protected readonly string RepositoryLocation;
        protected readonly string HomeFolder;
        public string TempClonePath { get; set; }

        protected GitRepository(string repositoryLocation, string tempClonePath, string homeFolder)
        {
            RepositoryLocation = repositoryLocation;
            TempClonePath = tempClonePath;
            HomeFolder = homeFolder;
        }

        public abstract void Push(string branchName);

        public bool ArchiveCurrentBranch(string zipFileLocation)
        {
            Log.Info($"Archive Current Branch into zip: {zipFileLocation}");
            var startInfo = GetStartInfo();
            startInfo.WorkingDirectory = TempClonePath;
            startInfo.Arguments = $@"archive --format=zip -o ""{zipFileLocation}"" HEAD";

            var process = new Process { StartInfo = startInfo };
            process.ErrorDataReceived += (sender, e) => { Log.Debug(e.Data); };
            process.OutputDataReceived += (sender, e) => { Log.Debug(e.Data); };
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();

            if (process.ExitCode == 0)
            {
                Log.Info("ArchiveCurrentBranch successful");
            }
            else
            {
                Log.Error($"Unable to ArchiveCurrentBranch , Exit code: {process.ExitCode}");
            }

            return (process.ExitCode == 0);
        }

        public bool ArchiveTreeIsh(string zipFileLocation,string treeIsh)
        {
            Log.Info($"Archive Git Tree: {treeIsh} into Zip: {zipFileLocation}");
            var startInfo = GetStartInfo();
            startInfo.WorkingDirectory = TempClonePath;
            startInfo.Arguments = $@"archive --format=zip -o ""{zipFileLocation}"" {treeIsh}";

            var process = new Process { StartInfo = startInfo };
            process.ErrorDataReceived += (sender, e) => { Log.Debug(e.Data); };
            process.OutputDataReceived += (sender, e) => { Log.Debug(e.Data); };
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();

            if (process.ExitCode == 0)
            {
                Log.Info("ArchiveTreeIsh successful");
            }
            else
            {
                Log.Error($"Unable to ArchiveTreeIsh , Exit code: {process.ExitCode}");
            }


            return (process.ExitCode == 0);

        }

        public Branch CheckoutBranch(string branchName)
        {
            Log.Info($"Checkout Branch: {branchName}");
            using (var repo = new Repository(TempClonePath))
            {
                Branch branch = repo.Branches.FirstOrDefault(b => b.Name == branchName && !b.IsRemote);
                if (branch == null)
                {
                    Log.Debug($"Local branch {branchName} cannot be found, looking for remote branch.");
                    string originBranch = $"origin/{branchName}";
                    var trackingBranch = repo.Branches.FirstOrDefault(b => b.Name == originBranch && b.IsRemote);
                    if (trackingBranch == null)
                        throw new Exception($"Remote branch: {originBranch} cannot be found, cannot create branch: {branchName} ");

                    Log.Debug($"Remote branch: {originBranch} found ");
                    branch = repo.CreateBranch(branchName, trackingBranch.Tip);
                    branch = repo.Branches.Update(branch, b => b.TrackedBranch = trackingBranch.CanonicalName);
                    repo.Checkout(branch);
                    return branch;
                }
                else
                {
                    repo.Checkout(branch);
                    return branch;
                }
            }
        }

        public Branch CheckoutMostRecentCommitBefore(string branchName, DateTime beforeDateTime)
        {
            Log.Trace($"Attempting to checkout the most recent commit before: {beforeDateTime}");
            using (var repo = new Repository(TempClonePath))
            {
                var commits = repo.Commits.QueryBy(new CommitFilter { Since = new[] { repo.Branches[branchName] } });

                var commitToCheckout = FindMostRecentCommitBefore(commits, beforeDateTime);
                if (commitToCheckout == null)
                {
                    Log.Warn($"Couldn't find any commits before {beforeDateTime}. Will use the most old commit when version synchronization was enabled instead.");
                    commitToCheckout = FindVersionSynchronizationEnabledCommit(commits);
                }

                Log.Trace($"Checking out commit \"{commitToCheckout.Message}\" ({commitToCheckout.Sha}) from {commitToCheckout.Author.When}");

                return repo.Checkout(commitToCheckout);
            }
        }

        private Commit FindMostRecentCommitBefore(ICommitLog commits, DateTime beforeDateTime)
        {
            var mostRecentCommit = commits
                .Where(c => c.Author.When < beforeDateTime)
                .Aggregate((Commit)null, (curMax, c) => (curMax == null || c.Author.When > curMax.Author.When ? c : curMax));

            return mostRecentCommit;
        }

        private Commit FindVersionSynchronizationEnabledCommit(ICommitLog commits)
        {
            var syncEnabledCommitMessage ="TeamCity change in '<Root project>' project: Synchronization with own VCS root is enabled";
            var syncEnabledCommit = commits.First(c=> c.Message == syncEnabledCommitMessage);

            if (syncEnabledCommit == null)
                throw new Exception("Cannot find commit with \"{syncEnabledCommitMessage}\" message.");

            return syncEnabledCommit;
        }


        public void DeleteFolder()
        {
            Log.Info($"Recursively Delete Folder: {TempClonePath}");

            var directoryInfo = new DirectoryInfo(TempClonePath);
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
            Log.Info($"Checking for Branch Existence: origin/{branchName}");
            using (var repo = new Repository(TempClonePath))
            {
                string originBranch = $"origin/{branchName}";
                var firstOrDefault = repo.Branches.FirstOrDefault(b => b.IsRemote && b.Name == originBranch);
                if (firstOrDefault==null)
                    Log.Info($"Branch origin/{branchName} does not exist");
                else
                {
                    Log.Warn($"Branch origin/{branchName} exists");
                }
                return firstOrDefault != null;
            }
        }

        public bool AddBranch(string branchName,string commitSha)
        {
            Log.Info(string.Format("Check to add Local Branch {0} tracking origin/{0} at specific commit: {1}", branchName, commitSha));
            if (CheckBranchExist(branchName))
            {
                Log.Warn($"Remote branch already exists: origin/{branchName}");
                return false;
            }

            using (var repo = new Repository(TempClonePath))
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
            Log.Error($"Failed to add local branch: {branchName}");
            return false;
        }

        public abstract bool Clone();

        public void StageAndCommit(IEnumerable<string> filesToStage, string message)
        {
            //Log.Trace($"Staging and committing files: {string.Join(", ", filesToStage)} with message: \"{message}\"");

            using (var repo = new Repository(TempClonePath))
            {
                foreach (var fileToStage in filesToStage)
                {
                    repo.Index.Add(fileToStage);
                }

                try
                {
                    var committer = new Signature("TeamCityConsole", "tcc@cwi", DateTimeOffset.Now);
                    repo.Commit(message, committer, committer);
                }
                catch (EmptyCommitException)
                {
                    //Don't want to explode on empty commit, as sometimes system would attempt to commit when no changes are done.
                    //for example when changing parameter to value which is already there
                }
            }
        }

        protected ProcessStartInfo GetStartInfo()
        {
            var startInfo = new ProcessStartInfo(@"git.exe");
            startInfo.EnvironmentVariables["HOME"] = HomeFolder;
            startInfo.EnvironmentVariables["USERPROFILE"] = HomeFolder;
            startInfo.EnvironmentVariables["HOMEPATH"] = HomeFolder;
            startInfo.UseShellExecute = false;
            startInfo.WorkingDirectory = Path.GetTempPath();
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;
            return startInfo;
        }
    }
}