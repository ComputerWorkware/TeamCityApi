using TeamCityApi.Domain;
using Xunit;

namespace TeamCityApi.Tests.Domain
{
    public class PropertyListTests
    {
        [Fact]
        public void ReplaceInString_should_do_basic_replacement()
        {
            var propertyList = new PropertyList()
            {
                new Property("a", "1"),
                new Property("b.c", "2")
            };

            var replaced = propertyList.ReplaceInString("hello %a%, %b.c%");

            Assert.Equal("hello 1, 2", replaced);
        }

        [Fact]
        public void ReplaceInString_should_replace_recursive_properties()
        {
            var propertyList = new PropertyList()
            {
                new Property("a", "%b%"),
                new Property("b", "%c%"),
                new Property("c", "3")
            };

            var replaced = propertyList.ReplaceInString("hello %a%");

            Assert.Equal("hello 3", replaced);
        }

        [Fact]
        public void ReplaceInString_should_not_touch_non_matched_properties()
        {
            var propertyList = new PropertyList()
            {
                new Property("bbbbbbb", "2222222"),
            };

            var replaced = propertyList.ReplaceInString("hello %a%");

            Assert.Equal("hello %a%", replaced);
        }
    }
}