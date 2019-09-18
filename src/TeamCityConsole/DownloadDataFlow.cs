using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using NLog;
using TeamCityConsole.Commands;
using TeamCityConsole.Utils;
using File = TeamCityApi.Domain.File;
using System;

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

        ActionBlock<PathFilePair> _downloadActionBlock;

        public Task Completion
        {
            get { return _downloadActionBlock.Completion; }
        }

        public DownloadDataFlow(IFileDownloader downloader)
        {
            _downloader = downloader;

            SetupDataFlow();
        }

        public void Complete()
        {
            _downloadActionBlock.Complete();
        }

        public void Download(PathFilePair pair)
        {
            _downloadActionBlock.Post(pair);
        }

        private void SetupDataFlow()
        {
            var options = new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = 8 };

            _downloadActionBlock = new ActionBlock<PathFilePair>(async pair => await HandleFileDownload(pair), options);
        }

        private async Task HandleFileDownload(PathFilePair pair)
        {
            Log.Debug("Downloading {0} to {1}", pair.File.Name, pair.Path);
            try
            {
                await _downloader.Download(pair.Path, pair.File);
            }
            catch (Exception ex)
            {
                Log.Debug("Exception Occurred: " + ex.ToString());
            }
            Log.Debug("Download complete: {0}", pair.File.Name);
        }

    }
}