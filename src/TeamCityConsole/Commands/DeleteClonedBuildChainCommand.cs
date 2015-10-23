using System;
using System.Threading.Tasks;
using NLog;
using TeamCityApi.UseCases;
using TeamCityConsole.Options;

namespace TeamCityConsole.Commands
{
    public class DeleteClonedBuildChainCommand : ICommand
    {
        private readonly DeleteClonedBuildChainUseCase _deleteClonedBuildChainUseCase;

        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public DeleteClonedBuildChainCommand(DeleteClonedBuildChainUseCase deleteClonedBuildChainUseCase)
        {
            _deleteClonedBuildChainUseCase = deleteClonedBuildChainUseCase;
        }

        public async Task Execute(object options)
        {
            var deleteClonedBuildChainOptions = options as DeleteClonedBuildChainOptions;
            if (deleteClonedBuildChainOptions == null) throw new ArgumentNullException("deleteClonedBuildChainOptions");

            Log.Info("BuildConfigId: " + deleteClonedBuildChainOptions.BuildConfigId);
            Log.Info("Simulate: " + deleteClonedBuildChainOptions.Simulate);

            await _deleteClonedBuildChainUseCase.Execute(deleteClonedBuildChainOptions.BuildConfigId, deleteClonedBuildChainOptions.Simulate);
            
            Log.Info("================Delete Cloned Build Chain: done ================");
        }
    }
}