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

        public async Task Execute(long buildId1, long buildId2, bool bCompare)
        {
            Log.Info("================Compare Builds: start ================");

            var build1 = await _client.Builds.ById(buildId1);
            var build2 = await _client.Builds.ById(buildId2);
            
            var buildChain1 = new BuildChain(_client.Builds, build1);
            var buildChain2 = new BuildChain(_client.Builds, build2);

            CompareBuilds(buildChain1, buildChain2, bCompare);
        }

        private void CompareBuilds(BuildChain buildChain1, BuildChain buildChain2, bool bCompare)
        {
            var build1List = buildChain1.Nodes.Select(node => node.Value.Properties.Property["project.name"]?.Value + " (" + node.Value.BuildConfig.ProjectName + ") - " + node.Value.Number).ToList();
            var build2List = buildChain2.Nodes.Select(node => node.Value.Properties.Property["project.name"]?.Value + " (" + node.Value.BuildConfig.ProjectName + ") - " + node.Value.Number).ToList();

            var build1Text = build1List.OrderBy(b => b).Aggregate("", (current, build) => current + (build + "\r\n"));
            var build2Text = build2List.OrderBy(b => b).Aggregate("", (current, build) => current + (build + "\r\n"));

            if (bCompare)
            {
                ShowDifferencesInBCompare(build1Text, build2Text);
            }
            else
            {
                ShowDifferencesInBrowser(build1Text, build2Text);
            }
        }

        private void ShowDifferencesInBCompare(string build1Text, string build2Text)
        {
            var mydocpath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            using (var outputFile = new StreamWriter(mydocpath + @"\buildList1.txt")) { outputFile.WriteLine(build1Text); }
            using (var outputFile = new StreamWriter(mydocpath + @"\buildList2.txt")) { outputFile.WriteLine(build2Text); }

            Process.Start(@"C:\Program Files (x86)\Beyond Compare 4\BCompare.exe", mydocpath + @"\buildList1.txt " + mydocpath + @"\buildList2.txt");
        }

        private void ShowDifferencesInBrowser(string build1Text, string build2Text)
        {
            var diff = new diff_match_patch();
            var linesResult = diff.diff_linesToChars(build1Text, build2Text);
            var lineText1 = linesResult[0];
            var lineText2 = linesResult[1];
            var lineArray = linesResult[2] as List<string>;
            var diffs = diff.diff_main(lineText1.ToString(), lineText2.ToString(), true);
            diff.diff_charsToLines(diffs, lineArray);

            var diffPrettyHtml = diff.diff_prettyHtmlSidebySide(diffs);

            var mydocpath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            using (var outputFile = new StreamWriter(mydocpath + @"\CompareBuilds.html")) { outputFile.WriteLine(diffPrettyHtml); }

            Process.Start("IExplore.exe", mydocpath + @"\CompareBuilds.html");
        }
    }
}