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

        public enum BuildChainFilter
        {
            All,
            Cloned,
            Original
        }

        private static readonly ILog Log = LogProvider.GetLogger(typeof(ShowBuildChainUseCase));

        private readonly ITeamCityClient _client;

        public ShowBuildChainUseCase(ITeamCityClient client)
        {
            _client = client;
        }

        public async Task Execute(string buildConfigId, BuildChainView view = BuildChainView.List, BuildChainFilter filter = BuildChainFilter.All)
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
                    Log.Info(Environment.NewLine + dependencyChain.ToString(filter));
                    break;
            }

            Log.Info("---------------------------------------------------------");

            var dependencies = dependencyChain.GetDependenciesWithMultipleVersions().ToList();
            if (dependencies.Any())
            {
                Log.Warn("There are some dependencies in the build chain with different versions:");

                foreach (var dependency in dependencies)
                {
                    Log.Warn(" - " + dependency.Key);
                    foreach (var buildNumber in dependency.Value)
                    {
                        Log.Warn("   - " + (buildNumber ?? "Same chain"));
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