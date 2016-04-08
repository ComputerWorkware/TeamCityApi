using System.Linq;
using System.Threading.Tasks;
using TeamCityApi.Helpers;
using TeamCityApi.Logging;

namespace TeamCityApi.UseCases
{
    public class ShowVersionsUseCase
    {
        private static readonly ILog Log = LogProvider.GetLogger(typeof(ShowVersionsUseCase));

        private readonly ITeamCityClient _client;

        public ShowVersionsUseCase(ITeamCityClient client)
        {
            _client = client;
        }

        public async Task Execute(string buildConfigId)
        {
            var buildConfig = await _client.BuildConfigs.GetByConfigurationId(buildConfigId);
            Log.Info($"Show versions for build config tree of {buildConfig.Id}");

            var buildConfigChain = new BuildConfigChain(_client.BuildConfigs, buildConfig);

            foreach (var node in buildConfigChain.Nodes)
            {
                Log.Info($"{node.Value.Id}: {node.Value.Parameters["MajorVersion"]?.Value}.{node.Value.Parameters["MinorVersion"]?.Value}");
            }
        }
    }
}