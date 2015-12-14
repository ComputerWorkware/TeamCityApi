using System;
using System.Xml;
using NSubstitute;
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

        public BuildConfigXmlGenerator(IBuildConfigXmlClient buildConfigXmlClient = null, bool buildNonStubVersion = false)
        {
            _buildConfigXmlClient = buildConfigXmlClient;

            if (buildNonStubVersion)
            {
                BuildConfigXml = new BuildConfigXml(_buildConfigXmlClient, Guid.NewGuid().ToString(), Guid.NewGuid().ToString());
            }
            else
            {
                BuildConfigXml = Substitute.For<BuildConfigXml>(_buildConfigXmlClient, Guid.NewGuid().ToString(), Guid.NewGuid().ToString());
            }

            Xml.AppendChild(Xml.CreateXmlDeclaration("1.0", "UTF-8", null));

            BuildTypeElement = (XmlElement)Xml.AppendChild(Xml.CreateElement("build-type"));
            BuildTypeElement.SetAttribute("uuid", Guid.NewGuid().ToString());

            NameElement = (XmlElement)BuildTypeElement.AppendChild(Xml.CreateElement("name"));

            SettingsElement = (XmlElement)BuildTypeElement.AppendChild(Xml.CreateElement("settings"));
            SettingsElement.SetAttribute("ref", "CPlusPlusTemplate_v1");

            ParametersElement = (XmlElement)SettingsElement.AppendChild(Xml.CreateElement("parameters"));
        }

        public BuildConfigXmlGenerator WithName(string name)
        {
            NameElement.InnerText = name;
            return this;
        }

        public BuildConfigXmlGenerator WithProjectId(string projectId)
        {
            BuildConfigXml.ProjectId = projectId;
            return this;
        }

        public BuildConfigXmlGenerator WithId(string buildConfigId)
        {
            BuildConfigXml.BuildConfigId = buildConfigId;
            return this;
        }

        public BuildConfigXmlGenerator WithTemplateId(string templateId)
        {
            SettingsElement = (XmlElement)BuildTypeElement.AppendChild(Xml.CreateElement("settings"));
            SettingsElement.SetAttribute("ref", templateId);
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

        public BuildConfigXmlGenerator WithParameters(Properties buildParameters)
        {
            foreach (var property in buildParameters.Property)
            {
                BuildConfigXml.SetParameterValue(property.Name, property.Value);
            }

            return this;
        }

        public BuildConfigXmlGenerator WithDependencies(DependencyDefinition[] dependencyDefinitions)
        {
            foreach (var dependencyDefinition in dependencyDefinitions)
            {
                var createArtifactDependency = new CreateArtifactDependency(BuildConfigXml.BuildConfigId, dependencyDefinition.SourceBuildConfig.Id);

                BuildConfigXml.CreateArtifactDependency(createArtifactDependency);
            }

            return this;
        }

        public BuildConfigXml Create()
        {
            return BuildConfigXml;
        }
    }
}