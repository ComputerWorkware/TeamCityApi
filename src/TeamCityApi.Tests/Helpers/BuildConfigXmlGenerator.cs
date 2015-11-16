using System;
using System.Xml;
using TeamCityApi.Clients;
using TeamCityApi.Domain;

namespace TeamCityApi.Tests.Helpers
{
    public class BuildConfigXmlGenerator
    {
        private readonly IBuildConfigXmlClient _buildConfigXmlClient;
        private BuildConfigXml BuildConfigXml { get; set; }
        private XmlDocument Xml => BuildConfigXml.Xml;
        private XmlElement BuildTypeElement { get; set; }
        private XmlElement NameElement { get; set; }
        private XmlElement SettingsElement { get; set; }
        private XmlElement ParametersElement { get; set; }
        private XmlElement ArtifactDependenciesElement { get; set; }
        private XmlElement DependenciesElement { get; set; }

        public BuildConfigXmlGenerator(IBuildConfigXmlClient buildConfigXmlClient = null)
        {
            _buildConfigXmlClient = buildConfigXmlClient;
            BuildConfigXml = new BuildConfigXml(_buildConfigXmlClient)
            {
                Xml = new XmlDocument()
            };

            BuildTypeElement = (XmlElement)Xml.AppendChild(Xml.CreateElement("build-type"));
            BuildTypeElement.SetAttribute("uuid", Guid.NewGuid().ToString());

            NameElement = (XmlElement)BuildTypeElement.AppendChild(Xml.CreateElement("name"));

            SettingsElement = (XmlElement)BuildTypeElement.AppendChild(Xml.CreateElement("settings"));
            SettingsElement.SetAttribute("ref", "CPlusPlusTemplate_v1");

            ParametersElement = (XmlElement)SettingsElement.AppendChild(Xml.CreateElement("parameters"));

            ArtifactDependenciesElement = (XmlElement)SettingsElement.AppendChild(Xml.CreateElement("artifact-dependencies"));

            DependenciesElement = (XmlElement)SettingsElement.AppendChild(Xml.CreateElement("dependencies"));
            
        }

        public BuildConfigXmlGenerator WithName(string name)
        {
            NameElement.InnerText = name;
            return this;
        }

        public BuildConfigXmlGenerator WithUuid(string uuid)
        {
            BuildTypeElement.SetAttribute("uuid", uuid);
            return this;
        }

        public BuildConfigXmlGenerator WithParameter(string name, string value)
        {
            var paramElement = (XmlElement)ParametersElement.AppendChild(Xml.CreateElement("param"));

            paramElement.SetAttribute("name", name);
            paramElement.SetAttribute("value", value);

            return this;
        }


        public BuildConfigXmlGenerator WithSnapshotDependency(CreateSnapshotDependency dependency)
        {
            BuildConfigXml.CreateSnapshotDependency(dependency);
            return this;
        }

        public BuildConfigXmlGenerator WithArtifactDependency(CreateArtifactDependency artifactDependency)
        {
            BuildConfigXml.CreateArtifactDependency(artifactDependency);
            return this;
        }

        public IBuildConfigXml Create()
        {
            return BuildConfigXml;
        }
    }
}