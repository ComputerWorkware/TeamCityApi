using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using TeamCityApi.Domain;
using TeamCityApi.Helpers.Git;
using TeamCityApi.Logging;
using TeamCityApi.UseCases;

namespace TeamCityApi.Clients
{
    public interface IBuildConfigXmlClient
    {
        /// <summary>
        /// Start tracking newly created BuildConfigXmls, to save them to disk on EndSetOfChanges
        /// </summary>
        /// <param name="buildConfigXml"></param>
        void Track(IBuildConfigXml buildConfigXml);
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
        /// <summary>
        /// Saves all tracked BuildConfigXmls to files, commits, pushes and deletes local repo.
        /// </summary>
        void EndSetOfChanges();
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
            var rootProject = _teamCityClient.Projects.GetById("_Root").Result;

            Log.Trace($"Read root project: {rootProject}");

            return rootProject.Properties[ParameterName.VersionedSettingGitRepo].Value;
        }

        public void Track(IBuildConfigXml buildConfigXml)
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

            return ReadBuildConfigXmlContents(projectId, buildConfigId);
        }

        private IBuildConfigXml ReadBuildConfigXmlContents(string projectId, string buildConfigId)
        {
            var xmlFileName = ConstructXmlFilePath(projectId, buildConfigId);
            Log.Debug($"Reading BuildConfig contents from {xmlFileName}");
            Log.Trace(System.IO.File.ReadAllText(xmlFileName));

            var buildConfigXml = new BuildConfigXml(this, projectId, buildConfigId);
            buildConfigXml.Xml.Load(xmlFileName);

            _buildConfigXmls.Add(buildConfigXml);

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

        public void EndSetOfChanges()
        {
            var filesToCommit = new List<string>();

            _gitRepository.CheckoutBranch("master");

            foreach (var buildConfigXml in _buildConfigXmls)
            {
                buildConfigXml.Xml.Save(ConstructXmlFilePath(buildConfigXml.ProjectId, buildConfigXml.BuildConfigId));
                filesToCommit.Add(ConstructXmlFileName(buildConfigXml.ProjectId, buildConfigXml.BuildConfigId));
            }

            _gitRepository.StageAndCommit(filesToCommit, "Changes by TeamCityConsole");

            _gitRepository.Push("master");

            _gitRepository.DeleteFolder();
        }
    }
}