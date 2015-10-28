using System.Linq;
using System.Threading.Tasks;
using TeamCityApi.Helpers;
using TeamCityApi.Logging;

namespace TeamCityApi.UseCases
{
    public class ShowBuildChainUseCase
    {
        private static readonly ILog Log = LogProvider.GetLogger(typeof(ShowBuildChainUseCase));

        private readonly ITeamCityClient _client;

        public ShowBuildChainUseCase(ITeamCityClient client)
        {
            _client = client;
        }

        public async Task Execute(string buildConfigId)
        {
            Log.Info("================Show Build Chain: start ================");

            var buildConfig = await _client.BuildConfigs.GetByConfigurationId(buildConfigId);
            var buildChainId = buildConfig.Parameters[ParameterName.BuildConfigChainId].Value;
            var buildConfigChain = new BuildConfigChain(_client.BuildConfigs, buildConfig);
            ShowBuildChain(buildConfigChain, buildChainId);
        }

        private static void ShowBuildChain(BuildConfigChain buildConfigChain, string buildChainId)
        {
            foreach (var node in buildConfigChain.Nodes.OrderBy(n=>n.Value.Id))
            {
                Log.InfoFormat(
                    !string.IsNullOrEmpty(node.Value.Parameters[ParameterName.ClonedFromBuildId]?.Value)
                        ? "BuildConfigId: (CLONED) {0}"
                        : "BuildConfigId: {0}", node.Value.Id);
            }
        }
    }
}