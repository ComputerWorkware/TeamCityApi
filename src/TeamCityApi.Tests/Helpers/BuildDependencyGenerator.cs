using System;
using System.Globalization;
using TeamCityApi.Domain;

namespace TeamCityApi.Tests.Helpers
{
    public class BuildDependencyGenerator
    {
        public static Dependency Artifact(Build b)
        {
            return new Dependency()
            {
                Id = long.Parse(b.Id),
                BuildTypeId = b.BuildTypeId,
                Number = b.Number,
                Status = BuildStatus.Success,
                State = "finished",
                Href = "/httpAuth/app/rest/builds/id:" + b.Id,
                WebUrl = "http://host:8080/viewLog.html?buildId="+ b.Id + "&buildTypeId=" + b.BuildTypeId
            };
        }

    }
}