using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TeamCityApi.Domain;
using TeamCityApi.Logging;

namespace TeamCityApi.UseCases
{
    public class ProjectInventoryUseCase
    {
        private const string NotAvailable = "N/A";

        private static readonly ILog Log = LogProvider.GetLogger(typeof(ProjectInventoryUseCase));

        private readonly ITeamCityClient _client;

        public ProjectInventoryUseCase(ITeamCityClient client)
        {
            _client = client;
        }

        public virtual async Task<string> Execute(long buildId)
        {
            var builds = await LoadSnapshotDependencyBuildChain(buildId);
            var inventoryRows = new List<ProjectInventoryRow>();

            foreach (var build in builds)
            {
                inventoryRows.AddRange(await LoadInventoryRows(build));
            }

            var orderedRows = inventoryRows
                .OrderBy(x => x.SolutionName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x.ProjectName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x.Type, StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x.Version, StringComparer.OrdinalIgnoreCase)
                .ToList();

            return RenderMarkdown(orderedRows);
        }

        private async Task<List<Build>> LoadSnapshotDependencyBuildChain(long buildId)
        {
            var buildSummaries = await _client.Builds.ByBuildLocator(locator => locator
                .WithSnapshotDependencyTo(buildId)
                .With("defaultFilter", "false")
                .WithCount(1000));

            var buildIds = buildSummaries
                .Select(x => x.Id)
                .Distinct()
                .ToList();

            if (!buildIds.Contains(buildId))
            {
                buildIds.Insert(0, buildId);
            }

            var buildTasks = buildIds.Select(x => _client.Builds.ById(x));
            var builds = await Task.WhenAll(buildTasks);

            return builds.ToList();
        }

        private async Task<List<ProjectInventoryRow>> LoadInventoryRows(Build build)
        {
            if (build == null)
            {
                throw new ArgumentNullException("build");
            }

            string solutionName = ResolveSolutionName(build);
            string solutionUrl = build.BuildConfig?.WebUrl ?? build.WebUrl;

            var resultingProperties = await _client.Builds.GetResultingProperties(build.Id);
            var inventoryProperty = resultingProperties?.Property?.FirstOrDefault(x =>
                string.Equals(x.Name, ParameterName.ProjectInventory, StringComparison.InvariantCultureIgnoreCase));

            if (string.IsNullOrWhiteSpace(inventoryProperty?.Value))
            {
                Log.Warn(string.Format("Build #{0} ({1}) is missing the \"{2}\" resulting property. Using N/A inventory values.", build.Id, solutionName, ParameterName.ProjectInventory));
                return new List<ProjectInventoryRow> { CreateFallbackRow(solutionName, solutionUrl) };
            }

            List<ProjectInventoryEntry> inventoryEntries;
            try
            {
                inventoryEntries = DeserializeInventoryEntries(inventoryProperty.Value);
            }
            catch (JsonException ex)
            {
                Log.Warn(string.Format("Build #{0} ({1}) has an invalid \"{2}\" resulting property value. Using N/A inventory values.", build.Id, solutionName, ParameterName.ProjectInventory));
                Log.Warn(ex.Message);
                return new List<ProjectInventoryRow> { CreateFallbackRow(solutionName, solutionUrl) };
            }

            if (inventoryEntries == null || inventoryEntries.Count == 0)
            {
                Log.Warn(string.Format("Build #{0} ({1}) has an empty \"{2}\" resulting property value. Using N/A inventory values.", build.Id, solutionName, ParameterName.ProjectInventory));
                return new List<ProjectInventoryRow> { CreateFallbackRow(solutionName, solutionUrl) };
            }

            var rows = inventoryEntries
                .Where(x => x != null)
                .Select(x => new ProjectInventoryRow
                {
                    SolutionName = solutionName,
                    SolutionUrl = solutionUrl,
                    ProjectName = ResolveFieldValue(x.Name),
                    Type = ResolveFieldValue(x.Type),
                    Version = ResolveFieldValue(x.Version)
                })
                .ToList();

            if (rows.Count == 0)
            {
                Log.Warn(string.Format("Build #{0} ({1}) contains no usable entries in \"{2}\". Using N/A inventory values.", build.Id, solutionName, ParameterName.ProjectInventory));
                return new List<ProjectInventoryRow> { CreateFallbackRow(solutionName, solutionUrl) };
            }

            return rows.Where(ShouldRenderRow).ToList();
        }

        private static string RenderMarkdown(IEnumerable<ProjectInventoryRow> inventoryRows)
        {
            var rows = inventoryRows.ToList();
            var builder = new StringBuilder();
            builder.AppendLine("| Solution | Project | Type | Version |");
            builder.AppendLine("| --- | --- | --- | --- |");

            foreach (var row in rows)
            {
                builder.AppendLine(string.Format("| {0} | {1} | {2} | {3} |",
                    FormatSolution(row),
                    EscapeMarkdownTableCell(row.ProjectName),
                    EscapeMarkdownTableCell(row.Type),
                    EscapeMarkdownTableCell(row.Version)));
            }

            builder.AppendLine();
            builder.AppendLine("| Type | Version | Count |");
            builder.AppendLine("| --- | --- | --- |");

            foreach (var summaryRow in BuildSummaryRows(rows))
            {
                builder.AppendLine(string.Format("| {0} | {1} | {2} |",
                    EscapeMarkdownTableCell(summaryRow.Type),
                    EscapeMarkdownTableCell(summaryRow.Version),
                    summaryRow.Count));
            }

            return builder.ToString();
        }

        private static string FormatSolution(ProjectInventoryRow row)
        {
            var solutionName = EscapeMarkdownLinkText(ResolveFieldValue(row.SolutionName));
            if (string.IsNullOrWhiteSpace(row.SolutionUrl))
            {
                return solutionName;
            }

            return string.Format("[{0}]({1})",
                solutionName,
                row.SolutionUrl.Replace("(", "%28").Replace(")", "%29"));
        }

        private static string EscapeMarkdownLinkText(string value)
        {
            return EscapeMarkdownTableCell(value)
                .Replace("[", "\\[")
                .Replace("]", "\\]");
        }

        private static string EscapeMarkdownTableCell(string value)
        {
            return (value ?? string.Empty)
                .Replace("|", "\\|")
                .Replace("\r\n", " ")
                .Replace("\n", " ")
                .Replace("\r", " ");
        }

        private static string ResolveSolutionName(Build build)
        {
            var solutionName = ResolveDirectProjectName(build.BuildConfig?.ProjectName) ?? build.BuildConfig?.Name ?? build.BuildTypeId;
            if (!string.IsNullOrWhiteSpace(solutionName))
            {
                return solutionName;
            }

            return string.Format("Build #{0}", build.Id);
        }

        private static string ResolveFieldValue(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? NotAvailable : value;
        }

        private static bool ShouldRenderRow(ProjectInventoryRow row)
        {
            if (string.Equals(row.Type, "Solution", StringComparison.InvariantCultureIgnoreCase))
            {
                return false;
            }

            return row.ProjectName.IndexOf("Tests", StringComparison.InvariantCultureIgnoreCase) < 0;
        }

        private static string ResolveDirectProjectName(string projectName)
        {
            if (string.IsNullOrWhiteSpace(projectName))
            {
                return null;
            }

            return projectName
                .Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .LastOrDefault();
        }

        private static IEnumerable<ProjectInventorySummaryRow> BuildSummaryRows(IEnumerable<ProjectInventoryRow> rows)
        {
            return rows
                .SelectMany(BuildSummaryRowsForProject)
                .GroupBy(x => new { x.Type, x.Version })
                .Select(x => new ProjectInventorySummaryRow
                {
                    Type = x.Key.Type,
                    Version = x.Key.Version,
                    Count = x.Count()
                })
                .OrderBy(x => x.Type, StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x.Version, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static IEnumerable<ProjectInventorySummaryRow> BuildSummaryRowsForProject(ProjectInventoryRow row)
        {
            var versions = NormalizeVersions(row.Version);
            foreach (var version in versions)
            {
                yield return new ProjectInventorySummaryRow
                {
                    Type = ResolveFieldValue(row.Type),
                    Version = version,
                    Count = 1
                };
            }
        }

        private static IEnumerable<string> NormalizeVersions(string versions)
        {
            var resolvedVersions = string.IsNullOrWhiteSpace(versions)
                ? new[] { NotAvailable }
                : versions.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

            return resolvedVersions
                .Select(NormalizeVersion)
                .Distinct(StringComparer.OrdinalIgnoreCase);
        }

        private static string NormalizeVersion(string version)
        {
            var normalizedVersion = ResolveFieldValue(version).Trim();
            if (string.Equals(normalizedVersion, NotAvailable, StringComparison.InvariantCultureIgnoreCase))
            {
                return NotAvailable;
            }

            var lowerVersion = normalizedVersion.ToLowerInvariant();
            foreach (var mapping in NetFrameworkVersionMappings)
            {
                if (Regex.IsMatch(lowerVersion, mapping.Key))
                {
                    return mapping.Value;
                }
            }

            if (Regex.IsMatch(lowerVersion, @"^netstandard\d+\.\d+$") ||
                Regex.IsMatch(lowerVersion, @"^net\d+(\.\d+)?$"))
            {
                return lowerVersion;
            }

            return normalizedVersion;
        }

        private static List<ProjectInventoryEntry> DeserializeInventoryEntries(string inventoryJson)
        {
            var token = JToken.Parse(inventoryJson);

            switch (token.Type)
            {
                case JTokenType.Array:
                    return token.ToObject<List<ProjectInventoryEntry>>();
                case JTokenType.Object:
                    return new List<ProjectInventoryEntry> { token.ToObject<ProjectInventoryEntry>() };
                default:
                    return null;
            }
        }

        private static ProjectInventoryRow CreateFallbackRow(string solutionName, string solutionUrl)
        {
            return new ProjectInventoryRow
            {
                SolutionName = ResolveFieldValue(solutionName),
                SolutionUrl = solutionUrl,
                ProjectName = NotAvailable,
                Type = NotAvailable,
                Version = NotAvailable
            };
        }

        private static readonly KeyValuePair<string, string>[] NetFrameworkVersionMappings =
        {
            new KeyValuePair<string, string>(@"^v?4\.8(\.\d+)*$", "net48"),
            new KeyValuePair<string, string>(@"^v?4\.7\.2(\.\d+)*$", "net472"),
            new KeyValuePair<string, string>(@"^v?4\.7\.1(\.\d+)*$", "net471"),
            new KeyValuePair<string, string>(@"^v?4\.7(\.\d+)*$", "net47"),
            new KeyValuePair<string, string>(@"^v?4\.6\.2(\.\d+)*$", "net462"),
            new KeyValuePair<string, string>(@"^v?4\.6\.1(\.\d+)*$", "net461"),
            new KeyValuePair<string, string>(@"^v?4\.6(\.\d+)*$", "net46"),
            new KeyValuePair<string, string>(@"^v?4\.5\.2(\.\d+)*$", "net452"),
            new KeyValuePair<string, string>(@"^v?4\.5\.1(\.\d+)*$", "net451"),
            new KeyValuePair<string, string>(@"^v?4\.5(\.\d+)*$", "net45")
        };
    }

    public class ProjectInventoryEntry
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("version")]
        public string Version { get; set; }
    }

    public class ProjectInventoryRow
    {
        public string SolutionName { get; set; }
        public string SolutionUrl { get; set; }
        public string ProjectName { get; set; }
        public string Type { get; set; }
        public string Version { get; set; }
    }

    public class ProjectInventorySummaryRow
    {
        public string Type { get; set; }
        public string Version { get; set; }
        public int Count { get; set; }
    }
}
