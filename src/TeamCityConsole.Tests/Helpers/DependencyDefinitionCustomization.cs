using System.Collections.Generic;
using Ploeh.AutoFixture;
using TeamCityApi.Domain;

namespace TeamCityConsole.Tests.Helpers
{
    public class DependencyDefinitionCustomization : ICustomization
    {
        public void Customize(IFixture fixture)
        {
            fixture.Customize<DependencyDefinition>(
                composer =>
                    composer.With(x => x.Properties,
                        new List<Property> {new Property {Name = "pathRules", Value = "file.dll => assemblies "}}));
        }
    }
}