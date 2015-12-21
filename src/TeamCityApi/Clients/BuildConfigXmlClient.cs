using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using TeamCityApi.Domain;
using TeamCityApi.Helpers.Git;
using TeamCityApi.Logging;
using TeamCityApi.UseCases;
using TeamCityApi.Util;

namespace TeamCityApi.Clients
{
    public interface IBuildConfigXmlClient
    {
        /// <summary>
        /// Read the most recent version of Build Config from TeamCity settings git repository.
        /// </summary>
        /// <param name="projectId"></param>
        /// <param name="buildConfigId"></param>
        /// <returns></returns>
        IBuildConfigXml Read(string projectId, string buildConfigId);
        /// <summary>
        /// Read Build Config from TeamCity settings git repository, based on the most recent commit before asOfDateTime
        /// </summary>
        /// <param name="projectId"></param>
        /// <param name="buildConfigId"></param>
        /// <param name="asOfDateTime"></param>
        /// <returns></returns>
        IBuildConfigXml ReadAsOf(string projectId, string buildConfigId, DateTime asOfDateTime);
        void Commit(IBuildConfigXml buildConfigXmlToCommit, string message);
        /// <summary>
        /// Push all changes and deletes local repo.
        /// </summary>
        void Push();

        bool Simulate { get; set; }
    }

    public class BuildConfigXmlClient : IBuildConfigXmlClient
    {
        private static readonly ILog Log = LogProvider.GetLogger(typeof(CloneRootBuildConfigUseCase));

        private readonly ITeamCityClient _teamCityClient;
        private readonly IGitRepository _gitRepository;
        private readonly List<IBuildConfigXml> _buildConfigXmls = new List<IBuildConfigXml>();

        public BuildConfigXmlClient(ITeamCityClient teamCityClient, IGitRepositoryFactory gitRepositoryFactory)
        {
            _teamCityClient = teamCityClient;
            _gitRepository = gitRepositoryFactory.Clone(GitAuthenticationType.Http, GetTeamCitySettingsRepositoryLocation());
        }

        private string GetTeamCitySettingsRepositoryLocation()
        {
            Project restHelperProject;
            try
            {
                restHelperProject = _teamCityClient.Projects.GetById("RestHelper").Result;
            }
            catch (ResourceNotFoundException e)
            {
                throw new Exception($"Required project with id: RestHelper was not found.", e);
            }

            if (!Simulate)
                Log.Trace($"Read RestHelper project: {restHelperProject}");

            var versionedSettingGitRepo = restHelperProject.Properties[ParameterName.VersionedSettingGitRepo].Value;

            if (String.IsNullOrEmpty(versionedSettingGitRepo))
            {
                throw new Exception($"Required {ParameterName.VersionedSettingGitRepo} parameter was not found on RestHelper project.");
            }

            return versionedSettingGitRepo;
        }

        public void IncludeInEndSetOfChanges(IBuildConfigXml buildConfigXml)
        {
            _buildConfigXmls.Add(buildConfigXml);
        }

        public IBuildConfigXml Read(string projectId, string buildConfigId)
        {
            _gitRepository.CheckoutBranch("master");

            return ReadBuildConfigXmlContents(projectId, buildConfigId);
        }

        public IBuildConfigXml ReadAsOf(string projectId, string buildConfigId, DateTime asOfDateTime)
        {
            _gitRepository.CheckoutMostRecentCommitBefore("master", asOfDateTime);

            var buildConfigXml = ReadBuildConfigXmlContents(projectId, buildConfigId);

            _gitRepository.CheckoutBranch("master");

            return buildConfigXml;
        }

        private IBuildConfigXml ReadBuildConfigXmlContents(string projectId, string buildConfigId)
        {
            var xmlFileName = ConstructXmlFilePath(projectId, buildConfigId);

            if (!Simulate)
            {
                Log.Debug($"Reading BuildConfig contents from {xmlFileName}");
                Log.Trace(System.IO.File.ReadAllText(xmlFileName));
            }

            var buildConfigXml = new BuildConfigXml(this, projectId, buildConfigId);
            buildConfigXml.Xml.Load(xmlFileName);

            return buildConfigXml;
        }

        private string ConstructXmlFileName(string projectId, string buildConfigId)
        {
            return Path.Combine(
                ".teamcity",
                projectId,
                "buildTypes",
                buildConfigId + ".xml");
        }

        private string ConstructXmlFilePath(string projectId, string buildConfigId)
        {
            return Path.Combine(
                _gitRepository.TempClonePath,
                ConstructXmlFileName(projectId, buildConfigId));
        }

        public void Commit(IBuildConfigXml buildConfigXmlToCommit, string message)
        {
            buildConfigXmlToCommit.Xml.Save(ConstructXmlFilePath(buildConfigXmlToCommit.ProjectId, buildConfigXmlToCommit.BuildConfigId));

            _gitRepository.StageAndCommit(new List<string>() { ConstructXmlFileName(buildConfigXmlToCommit.ProjectId, buildConfigXmlToCommit.BuildConfigId) }, message);
        }

        public void Push()
        {
            Log.Info($"==== Push changes to TeamCity's settings git repository ====");

            _gitRepository.Push("master");

            _gitRepository.DeleteFolder();
        }

        public bool Simulate { get; set; }
    }
}