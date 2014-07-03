using System;
using Ploeh.AutoFixture.Xunit;
using TeamCityApi.Locators;
using Xunit;
using Xunit.Extensions;

namespace TeamCityApi.Tests.Locators
{
    public class BuildLocatorTests
    {
        [Theory]
        [AutoData]
        public void Should_create_locator_string(string agentName)
        {
            var buildLocator = new BuildLocator();
            string locatorStr = buildLocator.WithAgentName(agentName).ToString();

            Assert.Equal("agentName:"+agentName, locatorStr);
        }

        [Theory]
        [AutoData]
        public void Should_overwrite_dimension_called_twice(
            string dimension, 
            string value1, 
            string value2)
        {
            string locatorStr = new BuildLocator()
                .With(dimension, value1)
                .With(dimension, value2).ToString();

            Assert.Equal(dimension+":"+value2, locatorStr);
        }

        [Theory]
        [AutoData]
        public void Should_separate_dimensions_with_commas(
            string dimension1,
            string dimension2,
            string value1,
            string value2)
        {
            string locatorStr = new BuildLocator()
                .With(dimension1, value1)
                .With(dimension2, value2).ToString();
            
            Assert.Equal(string.Format("{0}:{1},{2}:{3}", dimension1, value1, dimension2, value2), locatorStr);
        }
    }
}