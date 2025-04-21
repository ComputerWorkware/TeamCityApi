using System.Linq;
using System.Threading.Tasks;
using TeamCityApi.Helpers;
using TeamCityApi.Logging;

namespace TeamCityApi.UseCases
{
    public class PropagateVersionUseCase
    {
        private static readonly ILog Log = LogProvider.GetLogger(typeof(PropagateVersionUseCase));

        private readonly ITeamCityClient _client;

        public PropagateVersionUseCase(ITeamCityClient client)
        {
            _client = client;
        }

        public async Task Execute(string buildConfigId, bool simulate = false)
        {
            Log.Info("Propagate version.");

            var buildConfig = await _client.BuildConfigs.GetByConfigurationId(buildConfigId);
            var majorVersion = buildConfig.Parameters[ParameterName.MajorVersion].Value;
            var minorVersion = buildConfig.Parameters[ParameterName.MinorVersion].Value;
            var initialYear = buildConfig.Parameters[ParameterName.InitialYear].Value;
            var buildConfigChain = new BuildConfigChain(_client.BuildConfigs, buildConfig);
            await PropagateVersion(buildConfigChain, majorVersion, minorVersion, initialYear, simulate);
        }

        private async Task PropagateVersion(BuildConfigChain buildConfigChain, string majorVersion, string minorVersion, string initialYear, bool simulate)
        {
            foreach (var node in buildConfigChain.Nodes)
            {
                Log.Info($"Setting {node.Value.Id} to version {majorVersion}.{minorVersion} (Initial Year: {initialYear})");
                if (!simulate)
                {
                    //1st pass: set different, then in a project, value. Just to make parameter "own", see more: https://youtrack.jetbrains.com/issue/TW-42811
                    await _client.BuildConfigs.SetParameterValue(
                        l => l.WithId(node.Value.Id),
                        ParameterName.MajorVersion,
                        "Temporary value, different from the parent project value!"
                    );

                    //2ns pass: set real value.
                    await _client.BuildConfigs.SetParameterValue(
                        l => l.WithId(node.Value.Id),
                        ParameterName.MajorVersion,
                        majorVersion,
                        true
                    );

                    //1st pass: set different, then in a project, value. Just to make parameter "own", see more: https://youtrack.jetbrains.com/issue/TW-42811
                    await _client.BuildConfigs.SetParameterValue(
                        l => l.WithId(node.Value.Id),
                        ParameterName.MinorVersion,
                        "Temporary value, different from the parent project value!"
                    );

                    //2ns pass: set real value.
                    await _client.BuildConfigs.SetParameterValue(
                        l => l.WithId(node.Value.Id),
                        ParameterName.MinorVersion,
                        minorVersion,
                        true
                    );

                    //1st pass: set different, then in a project, value. Just to make parameter "own", see more: https://youtrack.jetbrains.com/issue/TW-42811
                    await _client.BuildConfigs.SetParameterValue(
                        l => l.WithId(node.Value.Id),
                        ParameterName.InitialYear,
                        "Temporary value, different from the parent project value!"
                    );

                    //2ns pass: set real value.
                    await _client.BuildConfigs.SetParameterValue(
                        l => l.WithId(node.Value.Id),
                        ParameterName.InitialYear,
                        initialYear,
                        true
                    );
                }
            }
        }
    }
}