using CommandLine;

namespace TeamCityConsole.Options
{
    [Verb(Verbs.GenerateEscrow, HelpText = "Generate Escrow")]
    public class GenerateEscrowOptions
    {
        [Option('i', "id", Required = true, HelpText = "Build Id for pulling escrow")]
        public int BuildId { get; set; }

        [Option('o', "OutputDir", Required = true, HelpText = "Output directory for escrow build folders")]
        public string OutputDirectory { get; set; }

        [Option('u', "user", Required = true, HelpText = "User for the git server to fetch repositories")]
        public string User { get; set; }

        [Option('p', "password", Required = true, HelpText = "Password for the git server to fetch repositories")]
        public string Password { get; set; }

        [Option('m', "manifestonly", Required = false, HelpText = "Generate Manifest document only.", DefaultValue = false)]
        public bool GenerateManifestOnly { get; set; }
    }
}