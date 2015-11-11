using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TeamCityApi.Domain;
using TeamCityApi.Helpers;
using TeamCityApi.Locators;

namespace TeamCityApi.Clients
{
    public interface IBuildConfigXmlClient
    {   
        void SetParameterValue(string buildConfigId, string name, string value);
        void CreateSnapshotDependency(CreateSnapshotDependency dependency);
        void CreateArtifactDependency(CreateArtifactDependency dependency);
        void DeleteSnapshotDependency(string buildConfigId, string dependencyBuildConfigId);
        void DeleteAllSnapshotDependencies(string buildConfigId);
        void FreezeAllArtifactDependencies(string buildConfigId, Build asOfbuild);
        void CreateDependency(string targetBuildConfigId, DependencyDefinition dependencyDefinition);
        void UpdateArtifactDependency(string buildConfigId, DependencyDefinition artifactDependency);
        void CopyBuildConfiguration(string sourceBuildTypeId, DateTime asOfDateTime, string newBuildTypeId, string newConfigurationName);
        void FreezeParameters(string buildConfigId, List<Property> sourceParameters);
        void Commit();
    }

    public class BuildConfigXmlClient : IBuildConfigXmlClient
    {
        private readonly IVcsRootHelper _vcsRootHelper;

        public BuildConfigXmlClient(IVcsRootHelper vcsRootHelper)
        {
            _vcsRootHelper = vcsRootHelper;
        }

        public void SetParameterValue(string buildConfigId, string name, string value)
        {
            throw new NotImplementedException();
        }

        public void CreateSnapshotDependency(CreateSnapshotDependency dependency)
        {
            throw new NotImplementedException();
        }

        public void CreateArtifactDependency(CreateArtifactDependency dependency)
        {
            throw new NotImplementedException();
        }

        public void DeleteSnapshotDependency(string buildConfigId, string dependencyBuildConfigId)
        {
            throw new NotImplementedException();
        }

        public void DeleteAllSnapshotDependencies(string buildConfigId)
        {
            throw new NotImplementedException();
        }

        public void FreezeAllArtifactDependencies(string buildConfigId, Build asOfbuild)
        {
            throw new NotImplementedException();
        }

        public void CreateDependency(string targetBuildConfigId, DependencyDefinition dependencyDefinition)
        {
            throw new NotImplementedException();
        }

        public void UpdateArtifactDependency(string buildConfigId, DependencyDefinition artifactDependency)
        {
            throw new NotImplementedException();
        }

        public void CopyBuildConfiguration(string sourceBuildTypeId, DateTime asOfDateTime, string newBuildTypeId,
            string newConfigurationName)
        {
            throw new NotImplementedException();
        }

        public void FreezeParameters(string buildConfigId, List<Property> sourceParameters)
        {
            throw new NotImplementedException();
        }

        public void Commit()
        {
            throw new NotImplementedException();
        }
    }
}