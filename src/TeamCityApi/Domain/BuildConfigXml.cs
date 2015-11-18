using System;
using System.Collections.Generic;
using System.Linq;
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
        IBuildConfigXml CopyBuildConfiguration(string newBuildTypeId, string newConfigurationName);
        void SetParameterValue(string name, string value);
        void CreateSnapshotDependency(CreateSnapshotDependency dependency);
        void CreateArtifactDependency(CreateArtifactDependency dependency);
        void DeleteSnapshotDependency(string dependencyBuildConfigId);
        void DeleteAllSnapshotDependencies();
        void FreezeAllArtifactDependencies(Build asOfbuild);
        void UpdateArtifactDependency(string sourceBuildTypeId, string revisionName, string revisionValue);
        void FreezeParameters(IEnumerable<Property> sourceParameters);
    }

    public class BuildConfigXml : IBuildConfigXml
    {
        private static readonly ILog Log = LogProvider.GetLogger(typeof(BuildConfigClient));

        private readonly IBuildConfigXmlClient _buildConfigXmlClient;
        public XmlDocument Xml { get; set; }
        public string BuildConfigId { get; set; }
        public string ProjectId { get; set; }
        private XmlElement BuildTypeElement => (XmlElement)Xml.SelectSingleNode("/build-type");
        private XmlElement ParametersElement => (XmlElement)Xml.SelectSingleNode("/build-type/settings/parameters");
        private XmlElement DependenciesElement => (XmlElement)Xml.SelectSingleNode("/build-type/settings/dependencies");
        private XmlElement ArtifactDependenciesElement => (XmlElement)Xml.SelectSingleNode("/build-type/settings/artifact-dependencies");

        public BuildConfigXml(IBuildConfigXmlClient buildConfigXmlClient, string projectId, string buildConfigId)
        {
            _buildConfigXmlClient = buildConfigXmlClient;

            Xml = new XmlDocument();

            BuildConfigId = buildConfigId;
            ProjectId = projectId;
        }

        public IBuildConfigXml CopyBuildConfiguration(string newBuildTypeId, string newConfigurationName)
        {
            Log.Trace($"XML CopyBuildConfiguration from {BuildConfigId} to {newBuildTypeId}");

            var clonedBuildConfigXml = new BuildConfigXml(_buildConfigXmlClient, ProjectId, newBuildTypeId);

            clonedBuildConfigXml.Xml.AppendChild(clonedBuildConfigXml.Xml.CreateXmlDeclaration("1.0", "UTF-8", null));

            var originalBuildTypeNode = clonedBuildConfigXml.Xml.ImportNode((XmlElement)Xml.SelectSingleNode("/build-type"), true);
            clonedBuildConfigXml.Xml.AppendChild(originalBuildTypeNode);

            var newBuildTypeElement = (XmlElement)clonedBuildConfigXml.Xml.SelectSingleNode("/build-type");
            newBuildTypeElement.SetAttribute("uuid", Guid.NewGuid().ToString());

            var newNameElement = (XmlElement)clonedBuildConfigXml.Xml.SelectSingleNode("/build-type/name");
            newNameElement.InnerText = newConfigurationName;

            _buildConfigXmlClient.IncludeInEndSetOfChanges(clonedBuildConfigXml);

            return clonedBuildConfigXml;
        }

        public void SetParameterValue(string name, string value)
        {
            Log.Trace($"XML SetParameterValue for: {BuildConfigId}, {name}: {value}");
            var paramElement = (XmlElement)Xml.SelectSingleNode("/build-type/settings/parameters/param[@name='" + name + "']");

            if (paramElement == null)
            {
                var newParamElement = (XmlElement)ParametersElement.AppendChild(Xml.CreateElement("param"));
                newParamElement.SetAttribute("name", name);
                newParamElement.SetAttribute("value", value);
            }
            else
            {
                paramElement.SetAttribute("value", value);
            }
        }

        public void CreateSnapshotDependency(CreateSnapshotDependency dependency)
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

        }

        public void CreateArtifactDependency(CreateArtifactDependency dependency)
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
        }

        public void DeleteSnapshotDependency(string dependencyBuildConfigId)
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
        }

        public void DeleteAllSnapshotDependencies()
        {
            Log.Debug($"XML DeleteAllSnapshotDependencies for: {BuildConfigId}");
            DependenciesElement.RemoveAll();
        }

        public void FreezeAllArtifactDependencies(Build asOfbuild)
        {
            Log.Debug($"XML FreezeAllArtifactDependencies for {BuildConfigId}, asOfbuild: {asOfbuild.Id}");

            var dependencyElements = ArtifactDependenciesElement.SelectNodes("dependency");

            if (asOfbuild.ArtifactDependencies == null)
            {
                throw new Exception($"Artifact dependencies for Build #{asOfbuild.Number} (id: {asOfbuild.Id}) unexpectedly empty");
            }

            foreach (XmlElement dependencyElement in dependencyElements)
            {
                var sourceBuildTypeId = dependencyElement.Attributes["sourceBuildTypeId"].Value;
                var buildNumber = asOfbuild.ArtifactDependencies.FirstOrDefault(a => a.BuildTypeId == sourceBuildTypeId).Number;
                UpdateArtifactDependency(sourceBuildTypeId, "buildNumber", buildNumber);
            }
        }

        public void UpdateArtifactDependency(string sourceBuildTypeId, string revisionName, string revisionValue)
        {
            Log.Trace($"XML BuildConfig.UpdateArtifactDependency for: {BuildConfigId}, sourceBuildTypeId: {sourceBuildTypeId}, revisionName: {revisionName}, revisionValue: {revisionValue}");

            var dependencyElement = ArtifactDependenciesElement.SelectSingleNode("dependency[@sourceBuildTypeId='" + sourceBuildTypeId + "']");
            var revisionRuleElement = dependencyElement.SelectSingleNode("revisionRule");

            revisionRuleElement.Attributes["name"].Value = revisionName;
            revisionRuleElement.Attributes["revision"].Value = revisionValue;
        }

        public void FreezeParameters(IEnumerable<Property> sourceParameters)
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