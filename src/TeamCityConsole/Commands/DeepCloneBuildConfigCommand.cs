using System;
using System.Threading.Tasks;
using NLog;
using TeamCityApi;
using TeamCityApi.UseCases;
using TeamCityConsole.Options;

namespace TeamCityConsole.Commands
{
    public class DeepCloneBuildConfigCommand : ICommand
    {
        private readonly DeepCloneBuildConfigUseCase _deepCloneBuildConfigUseCase;

        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public DeepCloneBuildConfigCommand(DeepCloneBuildConfigUseCase deepDeepCloneBuildConfigUseCase)
        {
            _deepCloneBuildConfigUseCase = deepDeepCloneBuildConfigUseCase;
        }

        public async Task Execute(object options)
        {
            var deepCloneBuildConfigOptions = options as DeepCloneBuildConfigOptions;
            if (deepCloneBuildConfigOptions == null)
                throw new ArgumentNullException("deepCloneBuildConfigOptions");

            await _deepCloneBuildConfigUseCase.Execute(deepCloneBuildConfigOptions.BuildId, deepCloneBuildConfigOptions.NewNameSuffix, deepCloneBuildConfigOptions.Simulate);

            Log.Info("================ Deep Clone Build Config: done ================");
        }
    }
}