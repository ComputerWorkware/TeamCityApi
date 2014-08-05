using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using NLog;
using TeamCityConsole.Commands;
using TeamCityConsole.Utils;
using File = TeamCityApi.Domain.File;

namespace TeamCityConsole
{
    public interface IDownloadDataFlow
    {
        Task Completion { get; }
        void Complete();
        void Download(PathFilePair pair);
    }

    public class DownloadDataFlow : IDownloadDataFlow
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private readonly IFileDownloader _downloader;

        ActionBlock<PathFilePair> _initialBroadcast;

        public Task Completion
        {
            get { return _initialBroadcast.Completion; }
        }

        public DownloadDataFlow(IFileDownloader downloader)
        {
            _downloader = downloader;

            SetupDataFlow();
        }

        public void Complete()
        {
            _initialBroadcast.Complete();
        }

        public void Download(PathFilePair pair)
        {
            _initialBroadcast.Post(pair);
        }

        private void SetupDataFlow()
        {
            var options = new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = 8 };

            _initialBroadcast = new ActionBlock<PathFilePair>(pair => HandleFileDownload(pair), options);
        }

        private void HandleFileDownload(PathFilePair pair)
        {
            Log.Debug("Downloading {0} to {1}", pair.File.Name, pair.Path);
            _downloader.Download(pair.Path, pair.File);
        }

    }
}