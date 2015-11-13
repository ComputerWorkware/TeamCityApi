using System;
using System.Collections.Generic;
using System.Xml;
using TeamCityApi.Clients;
using TeamCityApi.Helpers;
using TeamCityApi.Logging;

namespace TeamCityApi.Domain
{
    public interface IBuildConfigXml
    {
        XmlDocument Xml { get; set; }
        IBuildConfigXml CopyBuildConfiguration(string newBuildTypeId, string newConfigurationName);
        void SetParameterValue(string name, string value);
        void CreateSnapshotDependency(CreateSnapshotDependency dependency);
        void CreateArtifactDependency(CreateArtifactDependency dependency);
        void DeleteSnapshotDependency(string dependencyBuildConfigId);
        void DeleteAllSnapshotDependencies();
        void FreezeAllArtifactDependencies(Build asOfbuild);
        void UpdateArtifactDependency(UpdateArtifactDependency updates);
        void FreezeParameters(List<Property> sourceParameters);
    }

    public class BuildConfigXml : IBuildConfigXml
    {
        private static readonly ILog Log = LogProvider.GetLogger(typeof(BuildConfigClient));

        private readonly IBuildConfigXmlClient _buildConfigXmlClient;
        public XmlDocument Xml { get; set; }

        private XmlElement BuildTypeElement => (XmlElement)Xml.SelectSingleNode("/build-type");
        private XmlElement NameElement => (XmlElement)Xml.SelectSingleNode("/build-type/name");
        private XmlElement ParametersElement => (XmlElement)Xml.SelectSingleNode("/build-type/settings/parameters");
        private XmlElement DependenciesElement => (XmlElement)Xml.SelectSingleNode("/build-type/settings/dependencies");
        private XmlElement ArtifactDependenciesElement => (XmlElement)Xml.SelectSingleNode("/build-type/settings/artifact-dependencies");

        private string BuildConfigId => BuildTypeElement.Attributes["uuid"].Value;

        public BuildConfigXml(IBuildConfigXmlClient buildConfigXmlClient)
        {
            _buildConfigXmlClient = buildConfigXmlClient;

            Xml = new XmlDocument();
        }

        public IBuildConfigXml CopyBuildConfiguration(string newBuildTypeId, string newConfigurationName)
        {
            var clonedBuildConfigXml = new BuildConfigXml(_buildConfigXmlClient);
            
            var node = clonedBuildConfigXml.Xml.ImportNode(Xml.FirstChild, true);
            clonedBuildConfigXml.Xml.AppendChild(node);

            var newBuildTypeElement = (XmlElement)clonedBuildConfigXml.Xml.SelectSingleNode("/build-type");
            newBuildTypeElement.SetAttribute("uuid", newBuildTypeId);

            var newNameElement = (XmlElement)clonedBuildConfigXml.Xml.SelectSingleNode("/build-type/name");
            newNameElement.InnerText = newConfigurationName;

            _buildConfigXmlClient.Track(clonedBuildConfigXml);

            return clonedBuildConfigXml;
        }

        public void SetParameterValue(string name, string value)
        {
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
            DependenciesElement.RemoveAll();
        }

        public void FreezeAllArtifactDependencies(Build asOfbuild)
        {
            throw new NotImplementedException();
        }

        public void UpdateArtifactDependency(UpdateArtifactDependency updates)
        {
            var dependencyElement = ArtifactDependenciesElement.SelectSingleNode("dependency[@sourceBuildTypeId='" + updates.DependencyBuildConfigId + "']");
            var revisionRuleElement = dependencyElement.SelectSingleNode("revisionRule");
            var artifactElement = dependencyElement.SelectSingleNode("artifact");

            dependencyElement.Attributes["cleanDestination"].Value = updates.CleanDestinationDirectory.ToString().ToLower();
            revisionRuleElement.Attributes["name"].Value = updates.RevisionName;
            revisionRuleElement.Attributes["revision"].Value = updates.RevisionValue;
            artifactElement.Attributes["sourcePath"].Value = updates.PathRules;
        }

        public void FreezeParameters(List<Property> sourceParameters)
        {
            throw new NotImplementedException();
        }
    }
}