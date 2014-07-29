using System.Collections.Generic;
using System.Linq;
using Ploeh.AutoFixture.Xunit;
using TeamCityConsole.Commands;
using TeamCityConsole.Model;
using Xunit;
using Xunit.Extensions;

namespace TeamCityConsole.Tests
{
    public class DependencyConfigTests
    {
        public class Equals
        {
            [Theory]
            [AutoData]
            public void Should_return_true_if_identical(DependencyConfig config)
            {
                var otherConfig = new DependencyConfig()
                {
                    BuildConfigId = config.BuildConfigId,
                    BuildInfos = config.BuildInfos.ToList(),
                };

                Assert.Equal(config, otherConfig);
            }

            [Theory]
            [AutoData]
            public void Should_return_false_if_not_identical(DependencyConfig config, DependencyConfig otherConfig)
            {
                Assert.NotEqual(config, otherConfig);
            }

            [Theory]
            [AutoData]
            public void Should_return_true_if_identical_but_BuildInfos_are_ordered_different(DependencyConfig config)
            {
                List<BuildInfo> buildInfos = config.BuildInfos.ToList();
                buildInfos.Reverse();

                var otherConfig = new DependencyConfig()
                {
                    BuildConfigId = config.BuildConfigId,
                    BuildInfos = buildInfos,
                };

                Assert.Equal(config, otherConfig);
            }

            [Theory]
            [AutoData]
            public void Should_return_false_if_BuildInfos_has_an_additional_item(DependencyConfig config, BuildInfo buildInfo)
            {
                var otherConfig = new DependencyConfig()
                {
                    BuildConfigId = config.BuildConfigId,
                    BuildInfos = config.BuildInfos.ToList(),
                };

                //add the item that should make it fail
                otherConfig.BuildInfos.Add(buildInfo);

                Assert.NotEqual(config, otherConfig);
            }
        }
    }
}