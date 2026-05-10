using System;
using System.Threading.Tasks;
using NLog;
using TeamCityApi.UseCases;
using TeamCityConsole.Options;
using TeamCityConsole.Utils;

namespace TeamCityConsole.Commands
{
    public class ProjectInventoryCommand : ICommand
    {
        private const string DefaultOutputFileName = "project-inventory.md";

        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private readonly ProjectInventoryUseCase _projectInventoryUseCase;
        private readonly IFileSystem _fileSystem;

        public ProjectInventoryCommand(ProjectInventoryUseCase projectInventoryUseCase, IFileSystem fileSystem)
        {
            _projectInventoryUseCase = projectInventoryUseCase;
            _fileSystem = fileSystem;
        }

        public async Task Execute(object options)
        {
            var projectInventoryOptions = options as ProjectInventoryOptions;
            if (projectInventoryOptions == null) throw new ArgumentNullException("projectInventoryOptions");

            var outputPath = ResolveOutputPath(projectInventoryOptions);

            Log.Info("BuildId: " + projectInventoryOptions.BuildId);
            Log.Info("Output: " + outputPath);

            var markdown = await _projectInventoryUseCase.Execute(projectInventoryOptions.BuildId);

            _fileSystem.EnsureDirectoryExists(outputPath);
            _fileSystem.WriteAllTextToFile(outputPath, markdown);

            Log.Info("Saved project inventory to: " + outputPath);
            Log.Info("================ Project Inventory: done ================");
        }

        private string ResolveOutputPath(ProjectInventoryOptions options)
        {
            var outputFile = string.IsNullOrWhiteSpace(options.OutputFile)
                ? DefaultOutputFileName
                : options.OutputFile;

            return _fileSystem.GetFullPath(outputFile);
        }
    }
}
