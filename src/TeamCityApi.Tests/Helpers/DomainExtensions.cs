using System;
using System.Collections.Generic;
using System.Linq;
using Ploeh.AutoFixture.Dsl;
using TeamCityApi.Domain;

namespace TeamCityApi.Tests.Helpers
{
    public static class DomainExtensions
    {
        public static IPostprocessComposer<Build> WithId(
           this IPostprocessComposer<Build> composer, long id)
        {
            return composer.With(x => x.Id, id);
        }

        public static IPostprocessComposer<Build> WithBuildTypeId(
           this IPostprocessComposer<Build> composer, string id)
        {
            return composer.With(x => x.BuildTypeId, id);
        }

        public static IPostprocessComposer<Build> WithBuildConfigSummary(
           this IPostprocessComposer<Build> composer, BuildConfig buildConfig)
        {
            return composer
                .With(x => x.BuildConfig, (BuildConfigSummary) buildConfig)
                .With(x => x.BuildTypeId, buildConfig.Id);
        }

        public static IPostprocessComposer<Build> WithDependencies(
           this IPostprocessComposer<Build> composer, params Dependency[] dependencies)
        {
            return composer.With(x => x.ArtifactDependencies, new List<Dependency>(dependencies));
        }


        public static IPostprocessComposer<BuildConfig> WithNoDependencies(
           this IPostprocessComposer<BuildConfig> composer)
        {
            return composer.With(x => x.ArtifactDependencies, new List<DependencyDefinition>());
        }

        public static IPostprocessComposer<BuildConfig> WithDependencies(
           this IPostprocessComposer<BuildConfig> composer, params DependencyDefinition[] dependencyDefinitions)
        {
            return composer.With(x => x.ArtifactDependencies, new List<DependencyDefinition>(dependencyDefinitions));
        }

        public static IPostprocessComposer<BuildConfig> WithId(
           this IPostprocessComposer<BuildConfig> composer, string id)
        {
            return composer.With(x => x.Id, id);
        }

        public static IPostprocessComposer<BuildConfig> WithName(
           this IPostprocessComposer<BuildConfig> composer, string name)
        {
            return composer.With(x => x.Name, name);
        }

        public static IPostprocessComposer<BuildConfig> WithBuildConfigChainIdParameter(
           this IPostprocessComposer<BuildConfig> composer, string buildConfigChainId)
        {
            return composer.With(x => x.Parameters, new Properties()
            {
                Property = new PropertyList()
                {
                    new Property()
                    {
                        Name = ParameterName.BuildConfigChainId,
                        Own = true,
                        Value = buildConfigChainId
                    }
                }
            });
        }


        public static IPostprocessComposer<Project> WithId(
           this IPostprocessComposer<Project> composer, string id)
        {
            return composer.With(x => x.Id, id);
        }

        public static IPostprocessComposer<Project> WithBuildConfigSummary(
           this IPostprocessComposer<Project> composer, BuildConfig buildConfig)
        {
            return composer.With(x => x.BuildConfigs, new List<BuildConfigSummary>() { (BuildConfigSummary)buildConfig });
        }


        public static IPostprocessComposer<DependencyDefinition> WithPathRules(
           this IPostprocessComposer<DependencyDefinition> composer, string pathRules)
        {
            return composer.With(x => x.Properties, new Properties() {Count = "1", Property = new PropertyList { new Property { Name = "pathRules", Value = pathRules } }});
        }
    }
}