using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TeamCityApi.Domain;

namespace TeamCityApi.UseCases
{
    public class EscrowElement
    {
        public string ProjectName { get; set; }
        public string BuildTypeId { get; set; }
        public int Id { get; set; }
        public string Number { get; set; }
        public string VersionControlServer { get; set; }
        public string VersionControlPath { get; set; }
        public string VersionControlHash { get; set; }
        public string VersionControlBranch { get; set; }
        public int InitialYear { get; set; }
        public int MajorVersion { get; set; }
        public int MinorVersion { get; set; }
        public int RevisionVersion { get; set; }
        public int BuildNumberVersion { get; set; }
        public DateTime BuildStartDate { get; set; }
        public DateTime BuildFinishDate { get; set; }
        public string BuildAgentName { get; set; }
        public List<EscrowArtifactDependency> ArtifactDependencies { get; set; }

        public EscrowElement()
        {
            ArtifactDependencies = new List<EscrowArtifactDependency>();
        }
    }

    public class EscrowArtifactDependency
    {
        public string Number { get; set; }
        public long Id { get; set; }
        public string BuildTypeId { get; set; }
    }


    public class GenerateEscrowUseCase
    {
        private readonly ITeamCityClient _teamCityClient;

        public GenerateEscrowUseCase(ITeamCityClient teamCityClient)
        {
            _teamCityClient = teamCityClient;
        }
        public async Task<List<EscrowElement>> BuildEscrowList(int buildId)
        {
            var build1 = await _teamCityClient.Builds.ById(buildId);
            var buildChain1 = new TeamCityApi.Helpers.BuildChain(_teamCityClient.Builds, build1);

            List<EscrowElement> escrowElements = new List<EscrowElement>();
            foreach (var node in buildChain1.Nodes)
            {
                escrowElements.Add(MapBuildIntoEscrowElement(node.Value));
            }

            return escrowElements;
        }

        public async Task<bool> SaveDocument(List<EscrowElement> escrowElements, string fileName)
        {
            string escrowData = JsonConvert.SerializeObject(escrowElements, Formatting.Indented);

            var tw = System.IO.File.CreateText(fileName) as TextWriter;
            await tw.WriteAsync(escrowData);

            tw.Close();
            return true;
        }
        public async Task<bool> BuildEscrowFile(int buildId, string outputFileName)
        {
            var escrowElements = await BuildEscrowList(buildId);
            return await SaveDocument(escrowElements, outputFileName);
        }

        EscrowElement MapBuildIntoEscrowElement(Build build)
        {
            var element = new EscrowElement
            {
                BuildTypeId = build.BuildTypeId,
                Number = build.Number,
                BuildStartDate = build.StartDate,
                BuildFinishDate = build.FinishDate,
                BuildAgentName = build?.Agent?.Name,
                Id = Convert.ToInt32(build.Id)
            };

            element.InitialYear = GetIntProperty(build, "initial_year");
            element.MajorVersion = GetIntProperty(build, "majorversion");
            element.MinorVersion = GetIntProperty(build, "minorversion");
            element.ProjectName = build?.BuildConfig?.ProjectName;

            string[] versionElements = element.Number.Split('.');
            element.RevisionVersion = Convert.ToInt32(versionElements[2]);
            element.BuildNumberVersion = Convert.ToInt32(versionElements[3]);

            element.VersionControlServer = "http://cwigitlab.computerworkware.com/";

            if (build.Revisions.Count > 0)
            {
                element.VersionControlHash = build.Revisions[0]?.Version;
            }

            element.VersionControlPath = GetStringProperty(build, "git.repo.path");
            element.VersionControlBranch = GetStringProperty(build, "branch.name");

            if (build.ArtifactDependencies != null)
            {
                foreach (Dependency artifactDependency in build.ArtifactDependencies)
                {
                    element.ArtifactDependencies.Add(
                        new EscrowArtifactDependency()
                        {
                            BuildTypeId = artifactDependency.BuildTypeId,
                            Number = artifactDependency.Number,
                            Id = artifactDependency.Id
                        });
                }
            }

            return element;
        }

        public int GetIntProperty(Build build, string propertyName)
        {
            var property = build.Properties.Property.FirstOrDefault(x => string.Equals(propertyName, x.Name, StringComparison.InvariantCultureIgnoreCase));
            if (property == null)
                return default(int);
            int result = default(int);
            Int32.TryParse(property.Value, out result);
            return result;
        }
        public string GetStringProperty(Build build, string propertyName)
        {
            var property = build.Properties.Property.FirstOrDefault(x => string.Equals(propertyName, x.Name, StringComparison.InvariantCultureIgnoreCase));
            if (property == null)
                return string.Empty;
            return property.Value;
        }

    }

}