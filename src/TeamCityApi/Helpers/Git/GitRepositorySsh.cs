using System;
using System.Diagnostics;
using TeamCityApi.Logging;

namespace TeamCityApi.Helpers.Git
{
    public class GitRepositorySsh : GitRepository
    {
        private static readonly ILog Log = LogProvider.GetCurrentClassLogger();

        public GitRepositorySsh(string repositoryLocation, string tempClonePath, string sshKeyFolder) : base(repositoryLocation, tempClonePath, sshKeyFolder)
        {
        }

        public override void Push(string branchName)
        {
            Log.Debug($"Pushing Branch {branchName} using SSH Authentication");

            var startInfo = GetStartInfo();
            startInfo.WorkingDirectory = TempClonePath;
            startInfo.Arguments = $"push -u origin {branchName}";

            var process = new Process { StartInfo = startInfo };
            process.ErrorDataReceived += (sender, e) => { Log.Debug(e.Data); };
            process.OutputDataReceived += (sender, e) => { Log.Debug(e.Data); };
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();

            if (process.ExitCode == 0)
            {
                Log.Debug($"Branch {branchName} successfully pushed to remote.");
            }
            else
            {
                throw new Exception($"Unable to push branch: {branchName} , Exit code: {process.ExitCode}");
            }
        }

        public override bool Clone()
        {
            Log.Debug($"Clone repository with SSH: {RepositoryLocation} into {TempClonePath}");
            var startInfo = GetStartInfo();
            startInfo.Arguments = $"clone {RepositoryLocation} {TempClonePath}";

            var process = new Process { StartInfo = startInfo };
            process.ErrorDataReceived += (sender, e) => { Log.Debug(e.Data); };
            process.OutputDataReceived += (sender, e) => { Log.Debug(e.Data); };
            try
            {
                process.Start();
            }
            catch (Exception)
            {
                Log.Fatal("Cannot run git command. Make sure git is installed and in the PATH");
                throw;
            }

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();

            if (process.ExitCode == 0)
            {
                Log.Info($"Clone {RepositoryLocation} with SSH was successful");
            }
            else
            {
                Log.Error($"Failed to clone repository: {RepositoryLocation} , Exit code: {process.ExitCode}");
            }

            return (process.ExitCode == 0);
        }
    }
}