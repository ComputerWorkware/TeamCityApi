using System;
using System.Threading.Tasks;
using NLog;
using TeamCityApi.UseCases;
using TeamCityConsole.Options;

namespace TeamCityConsole.Commands
{
    public class ShowBuildChainCommand : ICommand
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private readonly ShowBuildChainUseCase _showBuildChainUseCase;

        public ShowBuildChainCommand(ShowBuildChainUseCase showBuildChainUseCase)
        {
            _showBuildChainUseCase = showBuildChainUseCase;
        }

        public async Task Execute(object options)
        {
            var showBuildChainOptions = options as ShowBuildChainOptions;
            if (showBuildChainOptions == null) throw new ArgumentNullException("showBuildChainOptions");

            Log.Info("BuildConfigId: " + showBuildChainOptions.BuildConfigId);

            await _showBuildChainUseCase.Execute(showBuildChainOptions.BuildConfigId, showBuildChainOptions.View);

            Log.Info("================Show Build Chain: done ================");
        }
    }
}