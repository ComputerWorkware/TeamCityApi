using System;
using System.Collections.Generic;
using Ploeh.AutoFixture.Dsl;
using TeamCityApi.Domain;
using TeamCityConsole.Options;

namespace TeamCityConsole.Tests.Helpers
{
    public static class DomainExtensions
    {
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

        public static IPostprocessComposer<DependencyDefinition> WithPathRules(
           this IPostprocessComposer<DependencyDefinition> composer, string pathRules)
        {
            return composer.With(x => x.Properties.Property, new List<DependencyProperty> { new DependencyProperty { Name = "pathRules", Value = pathRules } });
        }

        public static IPostprocessComposer<GetDependenciesOptions> WithForce(
           this IPostprocessComposer<GetDependenciesOptions> composer, string buildConfigId)
        {
            return composer.With(x => x.BuildConfigId, buildConfigId).With(x => x.Force, true);
        }
    }
}