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
            Log.Info("================ Show Build Chain: start ================");

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

            Log.Info("---------------------------------------------------------");

            var nonUniques = dependencyChain.GetNonUniqueDependencies().ToList();
            if (nonUniques.Any())
            {
                Log.Warn("There are some dependencies in the build chain with different versions:");

                foreach (var nonUnique in nonUniques)
                {
                    Log.Warn(" - " + nonUnique.Key.Id);
                    foreach (var build in nonUnique)
                    {
                        Log.Warn("   - " + (build != null ? build.Number : "Same chain"));
                    }
                }
            }
            else
            {
                Log.Info(" OK: Each dependency in the build chain has unique version.");
            }
        }
    }
}