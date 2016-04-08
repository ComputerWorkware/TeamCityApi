using System;
using System.Threading.Tasks;
using NLog;
using TeamCityApi.UseCases;
using TeamCityConsole.Options;

namespace TeamCityConsole.Commands
{
    public class PropagateVersionCommand : ICommand
    {
        private readonly PropagateVersionUseCase _propagateVersionUseCase;

        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public PropagateVersionCommand(PropagateVersionUseCase propagateVersionUseCase)
        {
            _propagateVersionUseCase = propagateVersionUseCase;
        }

        public async Task Execute(object options)
        {
            var propagateVersionOptions = options as PropagateVersionOptions;
            if (propagateVersionOptions == null) throw new ArgumentNullException("propagateVersionOptions");

            Log.Info("BuildConfigId: " + propagateVersionOptions.BuildConfigId);
            Log.Info("Simulate: " + propagateVersionOptions.Simulate);

            await _propagateVersionUseCase.Execute(propagateVersionOptions.BuildConfigId, propagateVersionOptions.Simulate);

            Log.Info("======================Propagate Version: done ===================");
        }
    }
}