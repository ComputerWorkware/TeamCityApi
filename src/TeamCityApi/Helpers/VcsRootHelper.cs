using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TeamCityApi.Clients;
using TeamCityApi.Domain;
using TeamCityApi.Logging;
using TeamCityApi.UseCases;

namespace TeamCityApi.Helpers
{
    class VcsRootHelper
    {
        private static readonly ILog Log = LogProvider.GetLogger(typeof(VcsRootHelper));

        private readonly BuildClient _buildClient;
        private readonly VcsRootClient _vcsRootClient;

        public VcsRootHelper(BuildClient buildClient, VcsRootClient vcsRootClient)
        {
            _buildClient = buildClient;
            _vcsRootClient = vcsRootClient;
        }

        public async Task<VcsCommit> GetCommitInformationByBuildId(string buildId)
        {
            Log.Info(string.Format("Get Commit Information for Build: {0}",buildId));
            Build build = await _buildClient.ById(buildId);

            Log.Debug("Build Loaded from TeamCity");

            string commitSha = build.Revisions.First().Version;

            Log.Debug(string.Format("Commit SHA from first Revision: {0}",commitSha));

            VcsRootInstance vcsRootInstance = build.Revisions.First().VcsRootInstance;

            Log.Debug(string.Format("Get VCSRoot by Id: {0}", vcsRootInstance.Id));
            VcsRoot vcsRoot = await _vcsRootClient.ById(vcsRootInstance.Id.ToString());

            Log.Debug(string.Format("VCSRoot: {0}",vcsRoot));
            VcsCommit commit = new VcsCommit(vcsRoot, commitSha);

            return commit;

        }

    }
}
