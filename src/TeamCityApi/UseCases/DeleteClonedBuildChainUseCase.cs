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
            var buildConfigIdsToDelete = buildConfigChain.Nodes
                .Where(node => node.Value.Parameters[ParameterName.BuildConfigChainId].Value == buildChainId)
                .Select(n => n.Value.Id)
                .ToList();

            buildConfigIdsToDelete.Reverse(); //to start deletion from leafs

            foreach (var buildConfigId in buildConfigIdsToDelete)
            {
                Log.InfoFormat("Deleting buildConfigId: {0}", buildConfigId);
                if (!simulate)
                {
                    await _client.BuildConfigs.DeleteBuildConfig(buildConfigId);
                }
            }
        }
    }
}