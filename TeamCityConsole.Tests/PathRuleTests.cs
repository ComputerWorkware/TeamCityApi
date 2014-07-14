using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TeamCityConsole.Utils;
using Xunit;
using File = TeamCityApi.Domain.File;

namespace TeamCityConsole.Tests
{
    public class PathRuleTests
    {
        public class Parse
        {
            [Fact]
            public void Should_handle_empty_strings()
            {
                List<PathRule> pathRules = PathRule.Parse(string.Empty);

                Assert.Empty(pathRules);
            }

            [Fact]
            public void Should_handle_empty_with_line_breaks()
            {
                List<PathRule> pathRules = PathRule.Parse(Environment.NewLine);

                Assert.Empty(pathRules);
            }

            [Fact]
            public void Should_handle_single_rule()
            {
                List<PathRule> pathRules = PathRule.Parse("source=>dest");

                PathRule pathRule = pathRules[0];

                Assert.Equal("source", pathRule.Source);
                Assert.Equal("dest", pathRule.Dest);
            }

            [Fact]
            public void Should_handle_single_rule_with_whitespace_padding()
            {
                List<PathRule> pathRules = PathRule.Parse("  source  => dest  ");

                PathRule pathRule = pathRules[0];

                Assert.Equal("source", pathRule.Source);
                Assert.Equal("dest", pathRule.Dest);
            }

            [Fact]
            public void Should_handle_single_rule_with_new_line()
            {
                List<PathRule> pathRules = PathRule.Parse("source=>dest"+Environment.NewLine);

                Assert.Equal(1, pathRules.Count);

                PathRule pathRule = pathRules[0];

                Assert.Equal("source", pathRule.Source);
                Assert.Equal("dest", pathRule.Dest);
            }

            [Fact]
            public void Should_handle_single_rule_with_source_only()
            {
                List<PathRule> pathRules = PathRule.Parse("source=>");

                PathRule pathRule = pathRules[0];

                Assert.Equal("source", pathRule.Source);
                Assert.Equal(string.Empty, pathRule.Dest);
            }

            [Fact]
            public void Should_handle_multiple_rules()
            {
                List<PathRule> pathRules = PathRule.Parse("source=>dest" + Environment.NewLine + "source2=>dest2");

                Assert.Equal(2, pathRules.Count);


                Assert.Equal("source", pathRules[0].Source);
                Assert.Equal("dest", pathRules[0].Dest);

                Assert.Equal("source2", pathRules[1].Source);
                Assert.Equal("dest2", pathRules[1].Dest);
            }

            [Fact]
            public void Should_handle_multiple_rules_with_left_side_only()
            {
                List<PathRule> pathRules = PathRule.Parse("source=>dest" + Environment.NewLine + "source2=>");

                Assert.Equal(2, pathRules.Count);


                Assert.Equal("source", pathRules[0].Source);
                Assert.Equal("dest", pathRules[0].Dest);

                Assert.Equal("source2", pathRules[1].Source);
                Assert.Equal(string.Empty, pathRules[1].Dest);
            }
        }

        public class ParseSource
        {
            [Fact]
            public void File_within_zip()
            {
                string source = "Cwi.Core-2.10-dev.zip!/Cwi.Core.dll";
                //string source = "en-us/BIFInstall_*.msi";
                //string source = "BIFLoader-2.9.7-dev.zip!**";
                var expected = new File
                {
                    ContentHref = source,
                    Name = "Cwi.Core.dll"
                };

                File file = PathRule.ParseSource(source);

                Assert.Equal(expected.ContentHref, file.ContentHref);
                Assert.Equal(expected.Name, file.Name);
            }

            [Fact]
            public void Root_file()
            {
                string source = "Cwi.Core.dll";

                var expected = new File
                {
                    ContentHref = source,
                    Name = "Cwi.Core.dll"
                };

                File file = PathRule.ParseSource(source);

                Assert.Equal(expected.ContentHref, file.ContentHref);
                Assert.Equal(expected.Name, file.Name);
            }

            [Fact]
            public void Directory()
            {
                string source = "/bin/release";

                var expected = new File
                {
                    ChildrenHref = source,
                    Name = "release"
                };

                File file = PathRule.ParseSource(source);

                Assert.Null(file.ContentHref);
                Assert.Equal(expected.ChildrenHref, file.ChildrenHref);
                Assert.Equal(expected.Name, file.Name);
            }
        }
    }
}
