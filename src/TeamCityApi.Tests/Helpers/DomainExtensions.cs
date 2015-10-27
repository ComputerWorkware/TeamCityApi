using System.Collections.Generic;
using Ploeh.AutoFixture.Dsl;
using TeamCityApi.Domain;

namespace TeamCityApi.Tests.Helpers
{
    public static class DomainExtensions
    {
        public static IPostprocessComposer<Build> WithId(
           this IPostprocessComposer<Build> composer, string id)
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
            return composer.With(x => x.BuildConfig, (BuildConfigSummary)buildConfig);
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