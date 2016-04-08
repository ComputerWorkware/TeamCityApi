using System;
using System.Threading.Tasks;
using NLog;
using TeamCityApi.UseCases;
using TeamCityConsole.Options;

namespace TeamCityConsole.Commands
{
    public class ShowVersionsCommand : ICommand
    {
        private readonly ShowVersionsUseCase _showVersionsUseCase;

        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public ShowVersionsCommand(ShowVersionsUseCase showVersionsUseCase)
        {
            _showVersionsUseCase = showVersionsUseCase;
        }

        public async Task Execute(object options)
        {
            var showVersionsOptions = options as ShowVersionsOptions;
            if (showVersionsOptions == null) throw new ArgumentNullException("showVersionsOptions");

            Log.Info("BuildConfigId: " + showVersionsOptions.BuildConfigId);

            await _showVersionsUseCase.Execute(showVersionsOptions.BuildConfigId);

            Log.Info("======================Show Versions: done ===================");
        }
    }
}