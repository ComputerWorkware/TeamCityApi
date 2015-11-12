using System;
using System.Collections.Generic;
using System.Xml;
using TeamCityApi.Clients;
using TeamCityApi.Helpers;

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
        void CreateDependency(DependencyDefinition dependencyDefinition);
        void UpdateArtifactDependency(DependencyDefinition artifactDependency);
        void FreezeParameters(List<Property> sourceParameters);
    }

    public class BuildConfigXml : IBuildConfigXml
    {
        private readonly IBuildConfigXmlClient _buildConfigXmlClient;
        public XmlDocument Xml { get; set; }

        public BuildConfigXml(IBuildConfigXmlClient buildConfigXmlClient)
        {
            _buildConfigXmlClient = buildConfigXmlClient;
        }

        public IBuildConfigXml CopyBuildConfiguration(string newBuildTypeId, string newConfigurationName)
        {
            var clonedBuildConfigXml = new BuildConfigXml(_buildConfigXmlClient);

            //todo: copy xml document, modify id and name

            _buildConfigXmlClient.Track(clonedBuildConfigXml);

            throw new NotImplementedException();
        }

        public void SetParameterValue(string name, string value)
        {
            var paramElement = (XmlElement)Xml.SelectSingleNode("/build-type/settings/parameters/param[@name='" + name + "']");

            if (paramElement == null)
            {
                var parametersNode = (XmlElement)Xml.SelectSingleNode("/build-type/settings/parameters");
                var newParamElement = (XmlElement)parametersNode.AppendChild(Xml.CreateElement("param"));
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
            throw new NotImplementedException();
        }

        public void CreateArtifactDependency(CreateArtifactDependency dependency)
        {
            throw new NotImplementedException();
        }

        public void DeleteSnapshotDependency(string dependencyBuildConfigId)
        {
            throw new NotImplementedException();
        }

        public void DeleteAllSnapshotDependencies()
        {
            throw new NotImplementedException();
        }

        public void FreezeAllArtifactDependencies(Build asOfbuild)
        {
            throw new NotImplementedException();
        }

        public void CreateDependency(DependencyDefinition dependencyDefinition)
        {
            throw new NotImplementedException();
        }

        public void UpdateArtifactDependency(DependencyDefinition artifactDependency)
        {
            throw new NotImplementedException();
        }

        public void FreezeParameters(List<Property> sourceParameters)
        {
            throw new NotImplementedException();
        }
    }
}