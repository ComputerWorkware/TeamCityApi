using System;
using System.Threading.Tasks;
using NLog;
using TeamCityApi.UseCases;
using TeamCityConsole.Options;

namespace TeamCityConsole.Commands
{
    public class CompareBuildsCommand : ICommand
    {
        private readonly CompareBuildsUseCase _compareBuildsUseCase;
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        
        public CompareBuildsCommand(CompareBuildsUseCase compareBuildsUseCase)
        {
            _compareBuildsUseCase = compareBuildsUseCase;
        }

        public async Task Execute(object options)
        {
            var compareBuildsOptions = options as CompareBuildsOptions;
            if (compareBuildsOptions == null) throw new ArgumentNullException("compareBuildsOptions");

            Log.Info("BuildId #1: " + compareBuildsOptions.NewBuildId + " -- BuildId #2: " + compareBuildsOptions.OldBuildId);

            await _compareBuildsUseCase.Execute(compareBuildsOptions.NewBuildId, compareBuildsOptions.OldBuildId, compareBuildsOptions.BCompare, compareBuildsOptions.Dump);

            Log.Info("================Compare Builds: done ================");
        }
    }
}