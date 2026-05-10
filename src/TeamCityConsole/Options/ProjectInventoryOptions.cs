using CommandLine;

namespace TeamCityConsole.Options
{
    [Verb(Verbs.ProjectInventory, HelpText = "Generate a markdown project inventory from a build snapshot chain.")]
    public class ProjectInventoryOptions
    {
        [Option('b', "buildId", Required = true, HelpText = "Root build id used to traverse the executed snapshot dependency chain.")]
        public long BuildId { get; set; }

        [Option('o', "output", Required = false, Default = "project-inventory.md", HelpText = "Output markdown file path. Defaults to project-inventory.md in the current directory.")]
        public string OutputFile { get; set; }
    }
}
