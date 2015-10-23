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

            Log.Info("BuildId #1: " + compareBuildsOptions.BuildId1 + " -- BuildId #2: " + compareBuildsOptions.BuildId2);

            await _compareBuildsUseCase.Execute(compareBuildsOptions.BuildId1, compareBuildsOptions.BuildId2);

            Log.Info("================Compare Builds: done ================");
        }
    }
}