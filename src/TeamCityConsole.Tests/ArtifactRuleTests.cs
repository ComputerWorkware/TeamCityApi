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
    public class ArtifactRuleTests
    {
        public class Parse
        {
            [Fact]
            public void Should_handle_empty_strings()
            {
                List<ArtifactRule> pathRules = ArtifactRule.Parse(string.Empty);

                Assert.Empty(pathRules);
            }

            [Fact]
            public void Should_handle_empty_with_line_breaks()
            {
                List<ArtifactRule> pathRules = ArtifactRule.Parse(Environment.NewLine);

                Assert.Empty(pathRules);
            }

            [Fact]
            public void Should_handle_single_rule()
            {
                List<ArtifactRule> pathRules = ArtifactRule.Parse("source=>dest");

                ArtifactRule artifactRule = pathRules[0];

                Assert.Equal("source", artifactRule.Source);
                Assert.Equal("dest", artifactRule.Dest);
            }

            [Fact]
            public void Should_handle_single_rule_with_whitespace_padding()
            {
                List<ArtifactRule> pathRules = ArtifactRule.Parse("  source  => dest  ");

                ArtifactRule artifactRule = pathRules[0];

                Assert.Equal("source", artifactRule.Source);
                Assert.Equal("dest", artifactRule.Dest);
            }

            [Fact]
            public void Should_handle_single_rule_with_new_line()
            {
                List<ArtifactRule> pathRules = ArtifactRule.Parse("source=>dest"+Environment.NewLine);

                Assert.Equal(1, pathRules.Count);

                ArtifactRule artifactRule = pathRules[0];

                Assert.Equal("source", artifactRule.Source);
                Assert.Equal("dest", artifactRule.Dest);
            }

            [Fact]
            public void Should_handle_single_rule_with_source_only()
            {
                List<ArtifactRule> pathRules = ArtifactRule.Parse("source=>");

                ArtifactRule artifactRule = pathRules[0];

                Assert.Equal("source", artifactRule.Source);
                Assert.Equal(string.Empty, artifactRule.Dest);
            }

            [Fact]
            public void Should_handle_multiple_rules()
            {
                List<ArtifactRule> pathRules = ArtifactRule.Parse("source=>dest" + Environment.NewLine + "source2=>dest2");

                Assert.Equal(2, pathRules.Count);


                Assert.Equal("source", pathRules[0].Source);
                Assert.Equal("dest", pathRules[0].Dest);

                Assert.Equal("source2", pathRules[1].Source);
                Assert.Equal("dest2", pathRules[1].Dest);
            }

            [Fact]
            public void Should_handle_multiple_rules_with_left_side_only()
            {
                List<ArtifactRule> pathRules = ArtifactRule.Parse("source=>dest" + Environment.NewLine + "source2=>");

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

                File file = ArtifactRule.ParseSource(source);

                Assert.Equal(expected.ContentHref, file.ContentHref);
                Assert.Equal(expected.Name, file.Name);
            }

            [Fact]
            public void Should_encode_white_spaces()
            {
                string source = "Cwi.Core-2.10-dev.zip!/My Path/Cwi.Core.dll";
                var expected = new File
                {
                    ContentHref = source,
                    Name = "Cwi.Core.dll"
                };

                File file = ArtifactRule.ParseSource(source);

                Assert.Equal("Cwi.Core-2.10-dev.zip!/My%20Path/Cwi.Core.dll", file.ContentHref);
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

                File file = ArtifactRule.ParseSource(source);

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

                File file = ArtifactRule.ParseSource(source);

                Assert.Null(file.ContentHref);
                Assert.Equal(expected.ChildrenHref, file.ChildrenHref);
                Assert.Equal(expected.Name, file.Name);
            }
        }
    }
}
