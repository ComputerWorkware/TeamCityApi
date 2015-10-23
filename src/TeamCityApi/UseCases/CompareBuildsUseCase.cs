using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TeamCityApi.Helpers;
using TeamCityApi.Logging;

namespace TeamCityApi.UseCases
{
    public class CompareBuildsUseCase
    {
        private static readonly ILog Log = LogProvider.GetLogger(typeof(CompareBuildsUseCase));

        private readonly ITeamCityClient _client;

        public CompareBuildsUseCase(ITeamCityClient client)
        {
            _client = client;
        }

        public async Task Execute(string buildId1, string buildId2)
        {
            Log.Info("================Compare Builds: start ================");

            var build1 = await _client.Builds.ById(buildId1);
            var build2 = await _client.Builds.ById(buildId2);
            
            var buildChain1 = new BuildChain(_client.Builds, build1);
            var buildChain2 = new BuildChain(_client.Builds, build2);

            CompareBuilds(buildChain1, buildChain2);
        }

        private void CompareBuilds(BuildChain buildChain1, BuildChain buildChain2)
        {
            var build1List = buildChain1.Nodes.Select(node => node.Value.Properties.Property.FirstOrDefault(p => p.Name == "project.name")?.Value + " - " + node.Value.Number).ToList();
            var build2List = buildChain2.Nodes.Select(node => node.Value.Properties.Property.FirstOrDefault(p => p.Name == "project.name")?.Value + " - " + node.Value.Number).ToList();

            var build1Text = build1List.Aggregate("", (current, build) => current + (build + "\r\n"));
            var build2Text = build2List.Aggregate("", (current, build) => current + (build + "\r\n"));

            var diffMatchPatch = new diff_match_patch();
            var diffMain = diffMatchPatch.diff_main(build1Text, build2Text);
            diffMatchPatch.diff_cleanupSemantic(diffMain);
            var diffPrettyHtml = diffMatchPatch.diff_prettyHtml(diffMain);

            string mydocpath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            using (StreamWriter outputFile = new StreamWriter(mydocpath + @"\CompareBuilds.html")) { outputFile.WriteLine(diffPrettyHtml); }
            
            Process.Start("IExplore.exe", mydocpath + @"\CompareBuilds.html");
        }
    }
}