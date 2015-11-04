using System;
using System.Linq;
using System.Threading.Tasks;
using TeamCityApi.Helpers;
using TeamCityApi.Logging;

namespace TeamCityApi.UseCases
{
    public class ShowBuildChainUseCase
    {
        public enum BuildChainView
        {
            List,
            Tree
        }

        private static readonly ILog Log = LogProvider.GetLogger(typeof(ShowBuildChainUseCase));

        private readonly ITeamCityClient _client;

        public ShowBuildChainUseCase(ITeamCityClient client)
        {
            _client = client;
        }

        public async Task Execute(string buildConfigId, BuildChainView view = BuildChainView.List)
        {
            Log.Info("================Show Build Chain: start ================");

            var buildConfig = await _client.BuildConfigs.GetByConfigurationId(buildConfigId);
            var dependencyChain = new DependencyChain(_client, buildConfig);

            switch (view)
            {
                case BuildChainView.Tree:
                    Log.Info(Environment.NewLine + dependencyChain.SketchGraph());
                    break;
                default:
                case BuildChainView.List:
                    Log.Info(Environment.NewLine + dependencyChain.ToString());
                    break;
            }
        }
    }
}