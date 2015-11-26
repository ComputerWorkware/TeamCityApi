using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using TeamCityApi.Clients;
using TeamCityApi.Helpers;
using TeamCityApi.Logging;

namespace TeamCityApi.Domain
{
    public interface IBuildConfigXml
    {
        XmlDocument Xml { get; set; }
        string BuildConfigId { get; set; }
        string ProjectId { get; set; }
        IBuildConfigXml CopyBuildConfiguration(string newBuildConfigId, string newConfigurationName);
        void SwitchTemplateAndRepoToCurrentState(BuildConfig currentBuildConfig);
        void SetParameterValue(string name, string value);
        void CreateSnapshotDependency(string sourceBuildTypeId);
        void CreateSnapshotDependency(CreateSnapshotDependency dependency);
        void CreateArtifactDependency(CreateArtifactDependency dependency);
        void DeleteSnapshotDependency(string dependencyBuildConfigId);
        void DeleteAllSnapshotDependencies();
        void FreezeAllArtifactDependencies(Build asOfbuild);
        void UpdateArtifactDependency(string sourceBuildTypeId, string newSourceBuildTypeId, string revisionName, string revisionValue);
        void FreezeParameters(IEnumerable<Property> sourceParameters);
    }

    public class BuildConfigXml : IBuildConfigXml
    {
        private static readonly ILog Log = LogProvider.GetLogger(typeof(BuildConfigClient));

        private readonly IBuildConfigXmlClient _buildConfigXmlClient;
        public XmlDocument Xml { get; set; }
        public string BuildConfigId { get; set; }
        public string ProjectId { get; set; }
        private XmlElement SettingsElement => (XmlElement)Xml.SelectSingleNode("/build-type/settings");
        private XmlElement BuildTypeElement => (XmlElement)Xml.SelectSingleNode("/build-type");
        private XmlElement ParametersElement => (XmlElement)Xml.SelectSingleNode("/build-type/settings/parameters");
        private XmlElement DependenciesElement
        {
            get
            {
                var dependenciesElement = (XmlElement)Xml.SelectSingleNode("/build-type/settings/dependencies");
                if (dependenciesElement == null)
                {
                    var settingsElement = (XmlElement)Xml.SelectSingleNode("/build-type/settings");
                    dependenciesElement = (XmlElement)settingsElement.AppendChild(Xml.CreateElement("dependencies"));
                }
                return dependenciesElement;
            }
        }

        private XmlElement ArtifactDependenciesElement
        {
            get
            {
                var artifactDependenciesElement = (XmlElement)Xml.SelectSingleNode("/build-type/settings/artifact-dependencies");
                if (artifactDependenciesElement == null)
                {
                    var settingsElement = (XmlElement)Xml.SelectSingleNode("/build-type/settings");
                    artifactDependenciesElement = (XmlElement)settingsElement.AppendChild(Xml.CreateElement("artifact-dependencies"));
                }
                return artifactDependenciesElement;
            }
        }

        public BuildConfigXml(IBuildConfigXmlClient buildConfigXmlClient, string projectId, string buildConfigId)
        {
            _buildConfigXmlClient = buildConfigXmlClient;

            Xml = new XmlDocument();

            BuildConfigId = buildConfigId;
            ProjectId = projectId;
        }

        public virtual IBuildConfigXml CopyBuildConfiguration(string newBuildConfigId, string newConfigurationName)
        {
            Log.Trace($"XML CopyBuildConfiguration from {BuildConfigId} to {newBuildConfigId}");

            var clonedBuildConfigXml = new BuildConfigXml(_buildConfigXmlClient, ProjectId, newBuildConfigId);

            clonedBuildConfigXml.Xml.AppendChild(clonedBuildConfigXml.Xml.CreateXmlDeclaration("1.0", "UTF-8", null));

            var originalBuildTypeNode = clonedBuildConfigXml.Xml.ImportNode((XmlElement)Xml.SelectSingleNode("/build-type"), true);
            clonedBuildConfigXml.Xml.AppendChild(originalBuildTypeNode);

            var newBuildTypeElement = (XmlElement)clonedBuildConfigXml.Xml.SelectSingleNode("/build-type");
            newBuildTypeElement.SetAttribute("uuid", Guid.NewGuid().ToString());

            var newNameElement = (XmlElement)clonedBuildConfigXml.Xml.SelectSingleNode("/build-type/name");
            newNameElement.InnerText = newConfigurationName;

            _buildConfigXmlClient.Commit(clonedBuildConfigXml, $"TCC {newBuildConfigId} Copy Build Config from {BuildConfigId} ");

            return clonedBuildConfigXml;
        }

        /// <summary>
        /// Ensure that cloned Build Config uses template used by current version of Build Config
        /// Because old templates might use VCS root, which is not available anymore
        /// </summary>
        /// <param name="currentBuildConfig"></param>
        public virtual void SwitchTemplateAndRepoToCurrentState(BuildConfig currentBuildConfig)
        {
            var settingsElement = (XmlElement)Xml.SelectSingleNode("/build-type/settings");
            var oldTemplateId = settingsElement.Attributes["ref"].Value;

            var newTemplateId = currentBuildConfig.Template.Id;

            if (oldTemplateId != newTemplateId)
            {
                Log.Warn($"XML Switch template on {BuildConfigId} from {oldTemplateId} to {newTemplateId}");

                settingsElement.SetAttribute("ref", currentBuildConfig.Template.Id);
                _buildConfigXmlClient.Commit(this, $"TCC {BuildConfigId} Switch template from {oldTemplateId} to {newTemplateId}");

                var currentGitRepoPathParameter = currentBuildConfig.Parameters[ParameterName.GitRepoPath];
                if (currentGitRepoPathParameter != null)
                {
                    SetParameterValue(ParameterName.GitRepoPath, currentBuildConfig.Parameters[ParameterName.GitRepoPath].Value);
                }
            }
        }

        public virtual void SetParameterValue(string name, string value)
        {
            Log.Trace($"XML SetParameterValue for: {BuildConfigId}, {name}: {value}");
            var paramElement = (XmlElement)Xml.SelectSingleNode("/build-type/settings/parameters/param[@name='" + name + "']");

            if (paramElement == null)
            {
                if (ParametersElement == null)
                {
                    SettingsElement.AppendChild(Xml.CreateElement("parameters"));
                }
                var newParamElement = (XmlElement)ParametersElement.AppendChild(Xml.CreateElement("param"));
                newParamElement.SetAttribute("name", name);
                newParamElement.SetAttribute("value", value);
            }
            else
            {
                paramElement.SetAttribute("value", value);
            }

            _buildConfigXmlClient.Commit(this, $"TCC {BuildConfigId} Set Parameter {name} = {value}");
        }

        public virtual void CreateSnapshotDependency(string sourceBuildTypeId)
        {
            CreateSnapshotDependency(new CreateSnapshotDependency(BuildConfigId, sourceBuildTypeId));
        }

        public virtual void CreateSnapshotDependency(CreateSnapshotDependency dependency)
        {
            Log.Debug($"XML CreateSnapshotDependency for: {dependency.TargetBuildConfigId}, to: {dependency.DependencyBuildConfigId}");

            var dependOnElement = (XmlElement)DependenciesElement.AppendChild(Xml.CreateElement("depend-on"));
            dependOnElement.SetAttribute("sourceBuildTypeId", dependency.DependencyBuildConfigId);

            var optionsElement = (XmlElement)dependOnElement.AppendChild(Xml.CreateElement("options"));

            var option1Element = (XmlElement)optionsElement.AppendChild(Xml.CreateElement("option"));
            option1Element.SetAttribute("name", "take-started-build-with-same-revisions");
            option1Element.SetAttribute("value", dependency.TakeStartedBuildWithSameRevisions.ToString().ToLower());

            var option2Element = (XmlElement)optionsElement.AppendChild(Xml.CreateElement("option"));
            option2Element.SetAttribute("name", "take-successful-builds-only");
            option2Element.SetAttribute("value", dependency.TakeSuccessFulBuildsOnly.ToString().ToLower());

            _buildConfigXmlClient.Commit(this, $"TCC {BuildConfigId} Create Snapshot Dependency to {dependency.DependencyBuildConfigId}");
        }

        public virtual void CreateArtifactDependency(CreateArtifactDependency dependency)
        {
            Log.Debug($"XML CreateArtifactDependency for: {BuildConfigId}, to: {dependency.DependencyBuildConfigId}");

            var dependencyElement = (XmlElement)ArtifactDependenciesElement.AppendChild(Xml.CreateElement("dependency"));
            dependencyElement.SetAttribute("sourceBuildTypeId", dependency.DependencyBuildConfigId);
            dependencyElement.SetAttribute("cleanDestination", dependency.CleanDestinationDirectory.ToString().ToLower());

            var revisionRuleElement = (XmlElement)dependencyElement.AppendChild(Xml.CreateElement("revisionRule"));
            revisionRuleElement.SetAttribute("name", dependency.RevisionName);
            revisionRuleElement.SetAttribute("revision", dependency.RevisionValue);

            var artifactElement = (XmlElement)dependencyElement.AppendChild(Xml.CreateElement("artifact"));
            artifactElement.SetAttribute("sourcePath", dependency.PathRules);

            _buildConfigXmlClient.Commit(this, $"TCC {BuildConfigId} Create Artifact Dependency to {dependency.DependencyBuildConfigId}");
        }

        public virtual void DeleteSnapshotDependency(string dependencyBuildConfigId)
        {
            Log.Trace($"XML DeleteSnapshotDependency for: {BuildConfigId}, to: {dependencyBuildConfigId}");

            var dependOnElement = Xml.SelectSingleNode("/build-type/settings/dependencies/depend-on[@sourceBuildTypeId='" + dependencyBuildConfigId + "']");
            if (dependOnElement == null)
            {
                Log.WarnFormat("Attempted to delete {0} snapshot dependency from {1}, which doesn't exist", dependencyBuildConfigId, BuildConfigId);
            }
            else
            {
                DependenciesElement.RemoveChild(dependOnElement);
            }
            _buildConfigXmlClient.Commit(this, $"TCC {BuildConfigId} Delete Snapshot Dependency to {dependencyBuildConfigId}");
        }

        public virtual void DeleteAllSnapshotDependencies()
        {
            Log.Debug($"XML DeleteAllSnapshotDependencies for: {BuildConfigId}");
            DependenciesElement.RemoveAll();
            _buildConfigXmlClient.Commit(this, $"TCC {BuildConfigId} Delete all snapshot dependencies");
        }

        public virtual void FreezeAllArtifactDependencies(Build asOfbuild)
        {
            Log.Debug($"XML FreezeAllArtifactDependencies for {BuildConfigId}, asOfbuild: {asOfbuild.Id}");

            var dependencyElements = ArtifactDependenciesElement.SelectNodes("dependency");

            foreach (XmlElement dependencyElement in dependencyElements)
            {
                var sourceBuildTypeId = dependencyElement.Attributes["sourceBuildTypeId"].Value;
                var buildDependencyByBuildConfigId = asOfbuild.ArtifactDependencies.FirstOrDefault(a => a.BuildTypeId == sourceBuildTypeId);
                if (buildDependencyByBuildConfigId == null)
                    throw new Exception($"Build #{asOfbuild.Number} (id: {asOfbuild.Id}) doesn't artifact contain dependency for {sourceBuildTypeId}. Found only following artifact dependencies: {Environment.NewLine + String.Join(Environment.NewLine, asOfbuild.ArtifactDependencies.Select(ad => ad.BuildTypeId))}");
                var buildNumber = buildDependencyByBuildConfigId.Number;
                UpdateArtifactDependency(sourceBuildTypeId, sourceBuildTypeId, "buildNumber", buildNumber);
            }

            _buildConfigXmlClient.Commit(this, $"TCC {BuildConfigId} Freeze all artifact dependencies for asOfbuild: {asOfbuild.Id}");
        }

        public virtual void UpdateArtifactDependency(string sourceBuildTypeId, string newSourceBuildTypeId, string revisionName, string revisionValue)
        {
            Log.Trace($"XML UpdateArtifactDependency for: {BuildConfigId}, sourceBuildTypeId: {sourceBuildTypeId}, newSourceBuildTypeId: {newSourceBuildTypeId}, revisionName: {revisionName}, revisionValue: {revisionValue}");

            var dependencyElement = ArtifactDependenciesElement.SelectSingleNode("dependency[@sourceBuildTypeId='" + sourceBuildTypeId + "']");

            if (dependencyElement == null)
                throw new Exception($"Cannot find artifact dependencies with sourceBuildTypeId == {sourceBuildTypeId}.");

            dependencyElement.Attributes["sourceBuildTypeId"].Value = newSourceBuildTypeId;

            var revisionRuleElement = dependencyElement.SelectSingleNode("revisionRule");
            revisionRuleElement.Attributes["name"].Value = revisionName;
            revisionRuleElement.Attributes["revision"].Value = revisionValue;

            _buildConfigXmlClient.Commit(this, $"TCC {BuildConfigId} Update artifact dependency. sourceBuildTypeId: {sourceBuildTypeId}, newSourceBuildTypeId: {newSourceBuildTypeId}, revisionName: {revisionName}, revisionValue: {revisionValue}");
        }

        public virtual void FreezeParameters(IEnumerable<Property> sourceParameters)
        {
            Log.Trace($"XML FreezeParameters for {BuildConfigId}, to: {sourceParameters}");

            var paramElements = ParametersElement.SelectNodes("param");

            foreach (XmlElement targetP in paramElements)
            {
                SetParameterValue(
                    targetP.Attributes["name"].Value,
                    sourceParameters.Single(sourceP => sourceP.Name == targetP.Attributes["name"].Value).Value
                );
            }
        }
    }
}