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

        BroadcastBlock<PathFilePair> _initialBroadcast;

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

            _initialBroadcast = new BroadcastBlock<PathFilePair>(pair => pair, options);

            var directoryTransformation = new TransformManyBlock<PathFilePair, PathFilePair>(pair => HandleDirectory(pair),
                options);

            var downloadActionBlock = new ActionBlock<PathFilePair>(pair => HandleFileDownload(pair), options);

            _initialBroadcast.LinkTo(directoryTransformation, pair => pair.File.HasChildren);
            directoryTransformation.LinkTo(_initialBroadcast);
            _initialBroadcast.LinkTo(downloadActionBlock, pair => pair.File.HasContent);
        }

        private IEnumerable<PathFilePair> HandleDirectory(PathFilePair pair)
        {
            List<File> children = pair.File.GetChildren().Result;
            IEnumerable<PathFilePair> childPairs = children.Select(x => new PathFilePair
            {
                File = x,
                Path = Path.Combine(pair.Path, x.Name)
            });

            return childPairs;
        }

        private void HandleFileDownload(PathFilePair pair)
        {
            Log.Debug("Downloading {0} to {1}", pair.File.Name, pair.Path);
            _downloader.Download(pair.Path, pair.File);
        }

    }
}