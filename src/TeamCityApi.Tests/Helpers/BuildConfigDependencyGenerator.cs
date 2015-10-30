using System;
using TeamCityApi.Domain;

namespace TeamCityApi.Tests.Helpers
{
    public class BuildConfigDependencyGenerator
    {

        public static DependencyDefinition ArtifactSameChain(BuildConfig bc)
        {
            return new DependencyDefinition()
            {
                Id = "Id" + Guid.NewGuid(),
                Properties = new Properties()
                {
                    Property = new PropertyList()
                    {
                        new Property("cleanDestinationDirectory", "true"),
                        new Property("pathRules", "**/*.*=>dependencies"),
                        new Property("revisionName", "sameChainOrLastFinished"),
                        new Property("revisionValue", "latest.sameChainOrLastFinished")
                    }
                },
                SourceBuildConfig = (BuildConfigSummary) bc,
                Type = "artifact_dependency"
            };
        }

        public static DependencyDefinition ArtifactFixedBuild(Build b)
        {
            return new DependencyDefinition()
            {
                Id = "Id" + Guid.NewGuid(),
                Properties = new Properties() {
                    Property = new PropertyList()
                    {
                        new Property("cleanDestinationDirectory", "true"),
                        new Property("pathRules", "**/*.*=>dependencies"),
                        new Property("revisionName", "buildNumber"),
                        new Property("revisionValue", b.Number)
                    }
                },
                SourceBuildConfig = b.BuildConfig,
                Type = "artifact_dependency"
            };
        }

    }
}