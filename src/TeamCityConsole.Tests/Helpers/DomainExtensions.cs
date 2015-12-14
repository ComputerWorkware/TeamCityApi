using System;
using System.Collections.Generic;
using Ploeh.AutoFixture.Dsl;
using TeamCityApi.Domain;
using TeamCityConsole.Options;

namespace TeamCityConsole.Tests.Helpers
{
    public static class DomainExtensions
    {
        public static IPostprocessComposer<GetDependenciesOptions> WithForce(
           this IPostprocessComposer<GetDependenciesOptions> composer, string buildConfigId)
        {
            return composer.With(x => x.BuildConfigId, buildConfigId).With(x => x.Force, true);
        }
    }
}