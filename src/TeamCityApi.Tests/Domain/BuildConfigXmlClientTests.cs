using TeamCityApi.Clients;
using TeamCityApi.Tests.Helpers;
using Xunit;
using Xunit.Extensions;

namespace TeamCityApi.Tests.Domain
{
    public class BuildConfigXmlClientTests
    {
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
    }
}