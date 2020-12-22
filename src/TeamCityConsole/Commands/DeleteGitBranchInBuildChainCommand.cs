using System;
using System.Threading.Tasks;
using NLog;
using TeamCityApi.UseCases;
using TeamCityConsole.Options;

namespace TeamCityConsole.Commands
{
    public class DeleteGitBranchInBuildChainCommand : ICommand
    {
        private readonly DeleteGitBranchesInBuildChainUseCase _deleteGitBranchesInBuildChainUseCase;

        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public DeleteGitBranchInBuildChainCommand(DeleteGitBranchesInBuildChainUseCase deleteGitBranchesInBuildChainUseCase)
        {
            _deleteGitBranchesInBuildChainUseCase = deleteGitBranchesInBuildChainUseCase;
        }

        public async Task Execute(object options)
        {
            var deleteGitBranchInBuildChainOptions = options as DeleteGitBranchInBuildChainOptions;
            if (deleteGitBranchInBuildChainOptions == null) throw new ArgumentNullException("deleteGitBranchInBuildChainOptions");

            Log.Info("BuildConfigId: " + deleteGitBranchInBuildChainOptions.BuildConfigId);
            Log.Info("Branch: " + deleteGitBranchInBuildChainOptions.Branch);
            Log.Info("Simulate: " + deleteGitBranchInBuildChainOptions.Simulate);            

            await _deleteGitBranchesInBuildChainUseCase.Execute(deleteGitBranchInBuildChainOptions.BuildConfigId, deleteGitBranchInBuildChainOptions.Branch, deleteGitBranchInBuildChainOptions.Simulate);
            
            Log.Info("================Delete Git Branch in Build Chain: done ================");
        }
    }
}