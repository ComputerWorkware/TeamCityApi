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
            var dependencyChain = new DependencyChain(_client, buildConfig);

            Log.Info(dependencyChain.ToString());
        }
        
    }
}