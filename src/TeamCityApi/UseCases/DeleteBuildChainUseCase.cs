using System.Linq;
using System.Threading.Tasks;
using TeamCityApi.Helpers;
using TeamCityApi.Logging;

namespace TeamCityApi.UseCases
{
    public class DeleteBuildChainUseCase
    {
        private static readonly ILog Log = LogProvider.GetLogger(typeof(CloneRootBuildConfigUseCase));

        private readonly ITeamCityClient _client;

        public DeleteBuildChainUseCase(ITeamCityClient client)
        {
            _client = client;
        }

        public async Task Execute(string buildConfigId, bool simulate = false)
        {
            Log.InfoFormat("Delete Build Chain.");

            var buildConfig = await _client.BuildConfigs.GetByConfigurationId(buildConfigId);
            var buildChainId = buildConfig.Parameters[ParameterName.BuildConfigChainId]?.Value;
            var buildConfigChain = new BuildConfigChain(_client.BuildConfigs, buildConfig);
            await DeleteBuildChain(buildConfigChain, buildChainId, simulate);
        }

        private async Task DeleteBuildChain(BuildConfigChain buildConfigChain, string buildChainId, bool simulate)
        {
            foreach (var node in buildConfigChain.Nodes.Where(node => node.Value.Parameters[ParameterName.BuildConfigChainId]?.Value == buildChainId))
            {
                Log.InfoFormat("Deleteing buildConfigId: {0}", node.Value.Id);
                if (!simulate)
                {
                    await _client.BuildConfigs.DeleteBuildConfig(node.Value.Id);
                }
            }
        }
    }
}