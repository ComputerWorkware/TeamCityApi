using System;
using System.Collections.Generic;
using System.Xml;
using NSubstitute;
using TeamCityApi.Clients;
using TeamCityApi.Domain;
using TeamCityApi.Tests.Helpers;
using Xunit;
using Xunit.Extensions;

namespace TeamCityApi.Tests.Domain
{
    public class BuildConfigXmlTests
    {
        [Theory]
        [AutoNSubstituteData]
        public void Should_copy_xml_config(string newBuildTypeId, string newConfigurationName, IBuildConfigXmlClient buildConfigXmlClient)
        {
            var original = new BuildConfigXmlGenerator(buildConfigXmlClient).Create();

            var clone = original.CopyBuildConfiguration(newBuildTypeId, newConfigurationName);
            var originalUuid = original.Xml.SelectSingleNode("/build-type").Attributes["uuid"].Value;
            var cloneUuid = clone.Xml.SelectSingleNode("/build-type").Attributes["uuid"].Value;

            Assert.NotEqual(originalUuid, cloneUuid);
            Assert.Equal(newBuildTypeId, clone.BuildConfigId);
            Assert.Equal(original.ProjectId, clone.ProjectId);
            Assert.Equal(newConfigurationName, clone.Xml.SelectSingleNode("/build-type/name").InnerText);
        }

        [Theory]
        [AutoNSubstituteData]
        public void Should_add_parameter_value_when_does_not_exists(string paramName, string paramVal)
        {
            var buildConfigXml = new BuildConfigXmlGenerator().Create();

            buildConfigXml.SetParameterValue(paramName, paramVal);

            Assert.Equal(paramVal, buildConfigXml.Xml.SelectSingleNode("/build-type/settings/parameters/param[@name='" + paramName +"']").Attributes["value"].Value);
        }

        [Theory]
        [AutoNSubstituteData]
        public void Should_update_existing_parameter_value(string paramName, string paramVal)
        {
            var buildConfigXml = new BuildConfigXmlGenerator()
                .WithParameter(paramName, "abc")
                .Create();

            buildConfigXml.SetParameterValue(paramName, paramVal);

            Assert.Equal(paramVal, buildConfigXml.Xml.SelectSingleNode("/build-type/settings/parameters/param[@name='" + paramName + "']").Attributes["value"].Value);
        }

        [Theory]
        [AutoNSubstituteData]
        public void Should_create_snapshot_dependency(string dependencyBuildConfigId, bool takeStartedBuildWithSameRevisions, bool takeSuccessFulBuildsOnly)
        {
            var buildConfigXml = new BuildConfigXmlGenerator().Create();

            var dependencyToCreate = new CreateSnapshotDependency(Arg.Any<string>(), dependencyBuildConfigId);
            dependencyToCreate.TakeStartedBuildWithSameRevisions = takeStartedBuildWithSameRevisions;
            dependencyToCreate.TakeSuccessFulBuildsOnly = takeSuccessFulBuildsOnly;

            buildConfigXml.CreateSnapshotDependency(dependencyToCreate);

            var dependOnElement = (XmlElement)buildConfigXml.Xml.SelectSingleNode("/build-type/settings/dependencies/depend-on[@sourceBuildTypeId='" + dependencyBuildConfigId + "']");
            Assert.Equal(dependencyBuildConfigId, dependOnElement.Attributes["sourceBuildTypeId"].Value);

            var option1 = dependOnElement.SelectSingleNode("options/option[@name='take-started-build-with-same-revisions']").Attributes["value"].Value;
            Assert.Equal(takeStartedBuildWithSameRevisions.ToString().ToLower(), option1);

            var option2 = dependOnElement.SelectSingleNode("options/option[@name='take-successful-builds-only']").Attributes["value"].Value;
            Assert.Equal(takeSuccessFulBuildsOnly.ToString().ToLower(), option2);
        }

        [Theory]
        [AutoNSubstituteData]
        public void Should_create_artifact_dependency(
            string dependencyBuildConfigId,
            bool cleanDestination,
            string revisionName,
            string revisionValue,
            string pathRules)
        {
            var buildConfigXml = new BuildConfigXmlGenerator().Create();

            var dependencyToCreate = new CreateArtifactDependency(Arg.Any<string>(), dependencyBuildConfigId);
            dependencyToCreate.CleanDestinationDirectory = cleanDestination;
            dependencyToCreate.RevisionName = revisionName;
            dependencyToCreate.RevisionValue = revisionValue;
            dependencyToCreate.PathRules = pathRules;

            buildConfigXml.CreateArtifactDependency(dependencyToCreate);

            var dependencyElement = (XmlElement)buildConfigXml.Xml.SelectSingleNode("/build-type/settings/artifact-dependencies/dependency[@sourceBuildTypeId='" + dependencyBuildConfigId + "']");
            Assert.Equal(dependencyBuildConfigId, dependencyElement?.Attributes["sourceBuildTypeId"].Value);
            Assert.Equal(cleanDestination.ToString().ToLower(), dependencyElement?.Attributes["cleanDestination"].Value);

            var revisionRuleElement = dependencyElement?.SelectSingleNode("revisionRule");
            Assert.Equal(revisionName, revisionRuleElement?.Attributes?["name"].Value);
            Assert.Equal(revisionValue, revisionRuleElement?.Attributes?["revision"].Value);

            var artifactElement = dependencyElement?.SelectSingleNode("artifact");
            Assert.Equal(pathRules, artifactElement?.Attributes?["sourcePath"].Value);
        }

        [Theory]
        [AutoNSubstituteData]
        public void Should_delete_snapshot_dependency(
            string buildConfigId,
            string dependencyBuildConfigId)
        {
            var buildConfigXml = new BuildConfigXmlGenerator()
                .WithSnapshotDependency(new CreateSnapshotDependency(Arg.Any<string>(), dependencyBuildConfigId))
                .Create();

            buildConfigXml.DeleteSnapshotDependency(dependencyBuildConfigId);

            var dependOnElement = (XmlElement)buildConfigXml.Xml.SelectSingleNode("/build-type/settings/dependencies/depend-on[@sourceBuildTypeId='" + dependencyBuildConfigId + "']");

            Assert.Null(dependOnElement);
        }

        [Theory]
        [AutoNSubstituteData]
        public void Should_delete_all_snapshot_dependencies(
            string buildConfigId,
            string dependencyBuildConfigId)
        {
            var buildConfigXml = new BuildConfigXmlGenerator()
                .WithSnapshotDependency(new CreateSnapshotDependency(Arg.Any<string>(), Arg.Any<string>()))
                .WithSnapshotDependency(new CreateSnapshotDependency(Arg.Any<string>(), Arg.Any<string>()))
                .Create();

            var dependenciesElement = (XmlElement)buildConfigXml.Xml.SelectSingleNode("/build-type/settings/dependencies");

            buildConfigXml.DeleteAllSnapshotDependencies();

            Assert.True(dependenciesElement.ChildNodes.Count == 0);
        }

        [Theory]
        [AutoNSubstituteData]
        public void Should_update_artifact_dependency(
            string dependencyBuildConfigId,
            CreateArtifactDependency before,
            string newSourceBuildConfigId,
            string newRevisionName,
            string newRevisionValue)
        {
            before.DependencyBuildConfigId = dependencyBuildConfigId;

            var buildConfigXml = new BuildConfigXmlGenerator()
                .WithArtifactDependency(before)
                .Create();

            buildConfigXml.UpdateArtifactDependency(dependencyBuildConfigId, newSourceBuildConfigId, newRevisionName, newRevisionValue);

            var dependencyElement = (XmlElement)buildConfigXml.Xml.SelectSingleNode("/build-type/settings/artifact-dependencies/dependency[@sourceBuildTypeId='" + newSourceBuildConfigId + "']");
            var revisionRuleElement = (XmlElement)dependencyElement?.SelectSingleNode("revisionRule");

            Assert.Equal(newRevisionName, revisionRuleElement?.Attributes["name"].Value);
            Assert.Equal(newRevisionValue, revisionRuleElement?.Attributes["revision"].Value);
        }

        [Theory]
        [AutoNSubstituteData]
        public void Should_freeze_all_artifact_dependencies(
            string dependencyBuildConfigId,
            CreateArtifactDependency artifactDependency,
            Build asOfBuild)
        {
            artifactDependency.DependencyBuildConfigId = dependencyBuildConfigId;
            asOfBuild.ArtifactDependencies[0].BuildTypeId = dependencyBuildConfigId;

            var buildConfigXml = new BuildConfigXmlGenerator()
                .WithArtifactDependency(artifactDependency)
                .Create();

            buildConfigXml.FreezeAllArtifactDependencies(asOfBuild);

            var dependencyElement = (XmlElement)buildConfigXml.Xml.SelectSingleNode("/build-type/settings/artifact-dependencies/dependency[@sourceBuildTypeId='" + dependencyBuildConfigId + "']");
            var revisionRuleElement = (XmlElement)dependencyElement?.SelectSingleNode("revisionRule");

            Assert.Equal("buildNumber", revisionRuleElement?.Attributes["name"].Value);
            Assert.Equal(asOfBuild.ArtifactDependencies[0].Number, revisionRuleElement?.Attributes["revision"].Value);
        }

        [Theory]
        [AutoNSubstituteData]
        public void Should_freeze_parameters(
            List<Property> sourceParameters)
        {
            var buildConfigXml = new BuildConfigXmlGenerator()
                .WithParameter(sourceParameters[0].Name, Arg.Any<string>())
                .Create();

            buildConfigXml.FreezeParameters(sourceParameters);

            var paramElement = (XmlElement)buildConfigXml.Xml.SelectSingleNode("/build-type/settings/parameters/param[@name='" + sourceParameters[0].Name + "']");

            Assert.Equal(sourceParameters[0].Value, paramElement.Attributes["value"].Value);
        }
    }
}