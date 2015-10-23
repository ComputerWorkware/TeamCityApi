using System;
using System.Threading.Tasks;
using NLog;
using TeamCityApi;
using TeamCityApi.UseCases;
using TeamCityConsole.Options;

namespace TeamCityConsole.Commands
{
    public class CloneChildBuildConfigCommand : ICommand
    {
        private readonly CloneChildBuildConfigUseCase _cloneChildBuildConfigUseCase;

        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public CloneChildBuildConfigCommand(CloneChildBuildConfigUseCase cloneChildBuildConfigUseCase)
        {
            _cloneChildBuildConfigUseCase = cloneChildBuildConfigUseCase;
        }

        public async Task Execute(object options)
        {
            var cloneBuildConfigOptions = options as CloneChildBuildConfigOptions;
            if (cloneBuildConfigOptions == null) throw new ArgumentNullException("cloneBuildConfigOptions");

            await _cloneChildBuildConfigUseCase.Execute(long.Parse(cloneBuildConfigOptions.BuildId), cloneBuildConfigOptions.CloneForRootBuildConfigId, cloneBuildConfigOptions.Simulate);

            Log.Info("================ Clone Build Config: done ================");
        }
    }
}