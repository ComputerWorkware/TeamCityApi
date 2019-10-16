using System;
using Ploeh.AutoFixture.Xunit;
using TeamCityApi.Fields;
using TeamCityApi.Locators;
using Xunit;
using Xunit.Extensions;

namespace TeamCityApi.Tests.Fields
{
    public class BuildsFieldsTests
    {
        [Theory]
        [AutoData]
        public void Should_create_deeply_nested_fields_string()
        {
            var buildsFields = new BuildsFields();

            buildsFields
                .WithLong()
                .WithBuildFields(b => b
                    .WithId()
                    .WithNumber()
                    .WithStatus()
                    .WithFinishDate()
                    .WithChangesFields(cs => cs
                        .WithChangeFields(c => c
                            .WithUserFields(u => u
                                .WithName()
                            )
                            .WithFilesFields(fs => fs
                                .WithFileFields(f => f
                                    .WithChangeType()
                                    .WithFile())
                            )
                        )
                    )
                );



            Assert.Equal("$long,build(id,number,status,finishDate,changes(change(user(name),files(file(changeType,file)))))", buildsFields.ToString());
        }
    }
}