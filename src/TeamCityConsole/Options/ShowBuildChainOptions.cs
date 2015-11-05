using CommandLine;
using TeamCityApi.UseCases;

namespace TeamCityConsole.Options
{
    [Verb(Verbs.ShowBuildChain, HelpText = "Shows build chain.")]
    public class ShowBuildChainOptions
    {
        [Option('c', "BuildConfigId", Required = true, HelpText = "Build Configuration Id.")]
        public string BuildConfigId { get; set; }

        [Option('v', "View", HelpText = "Can be \"List\" or \"Tree\", by default is \"List\".")]
        public ShowBuildChainUseCase.BuildChainView View { get; set; }

        [Option('f', "Filter", Required = false, HelpText = "Can be \"All\", \"Cloned\" or \"Original\", by default is \"All\". Works with List view only.")]
        public ShowBuildChainUseCase.BuildChainFilter Filter { get; set; }
    }
}