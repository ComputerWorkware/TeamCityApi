using System;
using System.Xml;
using TeamCityApi.Domain;

namespace TeamCityApi.Tests.Helpers
{
    public class BuildConfigXmlGenerator
    {
        private XmlDocument Xml { get; set; }
        private XmlElement BuildTypeElement { get; set; }
        private XmlElement NameElement { get; set; }
        private XmlElement SettingsElement { get; set; }
        private XmlElement ParametersElement { get; set; }
        private XmlElement ArtifactDependenciesElement { get; set; }
        private XmlElement DependenciesElement { get; set; }

        public BuildConfigXmlGenerator()
        {
            Xml = new XmlDocument();

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

            return this;
        }



        public IBuildConfigXml Create()
        {
            var buildConfigXml = new BuildConfigXml(null);
            buildConfigXml.Xml = Xml;
            return buildConfigXml;
        }


    }
}