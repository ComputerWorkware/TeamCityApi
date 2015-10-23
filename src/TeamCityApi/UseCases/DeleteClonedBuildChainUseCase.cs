using System.Linq;
using System.Threading.Tasks;
using TeamCityApi.Helpers;
using TeamCityApi.Logging;

namespace TeamCityApi.UseCases
{
    public class DeleteClonedBuildChainUseCase
    {
        private static readonly ILog Log = LogProvider.GetLogger(typeof(DeleteClonedBuildChainUseCase));

        private readonly ITeamCityClient _client;

        public DeleteClonedBuildChainUseCase(ITeamCityClient client)
        {
            _client = client;
        }

        public async Task Execute(string buildConfigId, bool simulate = false)
        {
            Log.InfoFormat("Delete Cloned Build Chain.");

            var buildConfig = await _client.BuildConfigs.GetByConfigurationId(buildConfigId);
            var buildChainId = buildConfig.Parameters[ParameterName.BuildConfigChainId].Value;
            var buildConfigChain = new BuildConfigChain(_client.BuildConfigs, buildConfig);
            await DeleteClonedBuildChain(buildConfigChain, buildChainId, simulate);
        }

        private async Task DeleteClonedBuildChain(BuildConfigChain buildConfigChain, string buildChainId, bool simulate)
        {
            foreach (var node in buildConfigChain.Nodes.Where(node => node.Value.Parameters[ParameterName.BuildConfigChainId].Value == buildChainId))
            {
                Log.InfoFormat("Deleting buildConfigId: {0}", node.Value.Id);
                if (!simulate)
                {
                    await _client.BuildConfigs.DeleteBuildConfig(node.Value.Id);
                }
            }
        }
    }
}