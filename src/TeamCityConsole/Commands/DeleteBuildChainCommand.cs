using System;
using System.Threading.Tasks;
using NLog;
using TeamCityApi.UseCases;
using TeamCityConsole.Options;

namespace TeamCityConsole.Commands
{
    public class DeleteBuildChainCommand : ICommand
    {
        private readonly DeleteBuildChainUseCase _deleteBuildChainUseCase;

        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public DeleteBuildChainCommand(DeleteBuildChainUseCase deleteBuildChainUseCase)
        {
            _deleteBuildChainUseCase = deleteBuildChainUseCase;
        }

        public async Task Execute(object options)
        {
            var deleteBuildChainOptions = options as DeleteBuildChainOptions;
            if (deleteBuildChainOptions == null) throw new ArgumentNullException("cloneBuildConfigOptions");

            Log.Info("BuildConfigId: " + deleteBuildChainOptions.BuildConfigId);
            Log.Info("Simulate: " + deleteBuildChainOptions.Simulate);

            await _deleteBuildChainUseCase.Execute(deleteBuildChainOptions.BuildConfigId, deleteBuildChainOptions.Simulate);
            
            Log.Info("================Delete Build Chain: done ================");
        }
    }
}