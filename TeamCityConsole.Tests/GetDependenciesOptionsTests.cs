using System;
using TeamCityConsole.Options;
using Xunit;

namespace TeamCityConsole.Tests
{
    public class GetDependenciesOptionsTests
    {
        public class Validate
        {
            [Fact]
            public void Should_fail_when_Force_true_and_BuildConfigId_missing()
            {
                var options = new GetDependenciesOptions { Force = true, BuildConfigId = null };

                Assert.Throws<Exception>(() => options.Validate());
            }
        }
    }
}