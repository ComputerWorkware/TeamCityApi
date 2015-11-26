using TeamCityApi.Domain;
using Xunit;

namespace TeamCityApi.Tests.Domain
{
    public class PropertyListTests
    {
        [Fact]
        public void Should_Replace_In_String()
        {
            var propertyList = new PropertyList()
            {
                new Property("a", "1"),
                new Property("b.c", "2")
            };

            var replaced = propertyList.ReplaceInString("hello %a%, %b.c%");

            Assert.Equal("hello 1, 2", replaced);
        }
    }
}