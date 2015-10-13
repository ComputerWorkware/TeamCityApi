using System;
using System.Threading.Tasks;
using NLog;
using TeamCityConsole.Options;

namespace TeamCityConsole.Commands
{
    public class CloneBuildConfigCommand : ICommand
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public async Task Execute(object options)
        {
            var cloneBuildConfigOptions = options as CloneBuildConfigOptions;
            if (cloneBuildConfigOptions == null) throw new ArgumentNullException("cloneBuildConfigOptions");

            if (cloneBuildConfigOptions.Mode == CloneBuildConfigOptions.CloneMode.Root)
                CloneRootBuildConfig(cloneBuildConfigOptions);

            if (cloneBuildConfigOptions.Mode == CloneBuildConfigOptions.CloneMode.Child)
                CloneChildBuildConfig(cloneBuildConfigOptions);

            Log.Info("================ Clone Build Config: done ================");
        }

        private void CloneRootBuildConfig(CloneBuildConfigOptions cloneBuildConfigOptions)
        {
            if (string.IsNullOrEmpty(cloneBuildConfigOptions.NewNameSuffix))
                throw new ArgumentNullException("newNameSuffix", "newNameSuffix should be provided in Root mode.");

            Log.Info("Cloning root build config. buildId: {0}, New Name Suffix: {1}, ", cloneBuildConfigOptions.BuildId, cloneBuildConfigOptions.NewNameSuffix);

        }

        private void CloneChildBuildConfig(CloneBuildConfigOptions cloneBuildConfigOptions)
        {
            if (string.IsNullOrEmpty(cloneBuildConfigOptions.CloneForRootBuildConfigId))
                throw new ArgumentNullException("cloneForRootBuildConfigId", "cloneForRootBuildConfigId should be provided in Child mode.");

            Log.Info("Cloning child build config. buildId: {0}, cloneForRootBuildConfigId: {1}", cloneBuildConfigOptions.BuildId, cloneBuildConfigOptions.CloneForRootBuildConfigId);

        }

        
    }
}