using System;
using System.Threading.Tasks;
using NLog;
using TeamCityApi;
using TeamCityApi.UseCases;
using TeamCityConsole.Options;

namespace TeamCityConsole.Commands
{
    public class CloneRootBuildConfigCommand : ICommand
    {
        private readonly CloneRootBuildConfigUseCase _cloneRootBuildConfigUseCase;

        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public CloneRootBuildConfigCommand(CloneRootBuildConfigUseCase cloneRootBuildConfigUseCase)
        {
            _cloneRootBuildConfigUseCase = cloneRootBuildConfigUseCase;
        }

        public async Task Execute(object options)
        {
            var cloneBuildConfigOptions = options as CloneRootBuildConfigOptions;
            if (cloneBuildConfigOptions == null) throw new ArgumentNullException("cloneBuildConfigOptions");

            await _cloneRootBuildConfigUseCase.Execute(cloneBuildConfigOptions.BuildId, cloneBuildConfigOptions.NewNameSuffix, cloneBuildConfigOptions.Simulate);

            Log.Info("================ Clone Build Config: done ================");
        }
    }
}