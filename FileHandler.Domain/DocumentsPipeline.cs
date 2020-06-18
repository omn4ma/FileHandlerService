using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace FileHandler.Domain
{
    public class DocumentsPipeline
    {
        private readonly string processingFolderPath;
        private readonly string resultFolderPath;
        private readonly int maxDegreeOfParallelism;
        private readonly CancellationToken token;
        private TransformBlock<string, string> startBlock;
        private ActionBlock<string> finishBlock;

        public Action<string> OnFinished { get; set; }

        public DocumentsPipeline(string processingPath, string resultPath, int maxDegreeOfParallelism, CancellationToken token)
        {
            this.processingFolderPath = processingPath;
            this.resultFolderPath = resultPath;
            this.maxDegreeOfParallelism = maxDegreeOfParallelism;
            this.token = token;
            InitializeBlocks();
        }

        public async Task Processing(string pathToFile)
        {
            await startBlock.SendAsync(pathToFile);
        }

        public async Task Complete()
        {
            startBlock.Complete();

            await finishBlock.Completion;
        }

        void InitializeBlocks()
        {
            var linkOptions = new DataflowLinkOptions { PropagateCompletion = true };
            var executeOptions = new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = maxDegreeOfParallelism };
            int i = 0; int j = 0;
            var moveBlock = new TransformBlock<string, string>(sourceFilePath =>
            {
                var destinationFilePath = Path.Combine(processingFolderPath, Path.GetFileName(sourceFilePath));

                using (var from = File.OpenRead(sourceFilePath))
                using (var to = File.OpenWrite(destinationFilePath))
                {
                    from.CopyTo(to);
                }

                return destinationFilePath;
            }, new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = maxDegreeOfParallelism, CancellationToken = token });


            var broadcastTempFileBlock = new BroadcastBlock<string>(s => s, executeOptions);

            var readBlock = new TransformBlock<string, string>(async path => await File.ReadAllTextAsync(path), executeOptions);

            var aggregateBlock = new TransformBlock<string, int>(text => text.Where(symbol => Char.IsLetter(symbol)).Count(), executeOptions);

            var joinBlock = new JoinBlock<string, int>();

            var deleteTempFileBlock = new TransformBlock<Tuple<string, int>, Tuple<string, int>>(data => {
                File.Delete(data.Item1);
                return data;
            });

            var saveBlock = new TransformBlock<Tuple<string, int>, string>(async data =>
            {
                var path = Path.Combine(resultFolderPath, Path.GetFileName(data.Item1));
                await File.WriteAllTextAsync(path, data.Item2.ToString());

                return path;
            }, executeOptions);

            var raiseNotificationBlock = new ActionBlock<string>(s => { if (OnFinished != null) { OnFinished(s); } }, executeOptions);

            moveBlock.LinkTo(broadcastTempFileBlock, linkOptions);
            broadcastTempFileBlock.LinkTo(readBlock, linkOptions);
            broadcastTempFileBlock.LinkTo(joinBlock.Target1, linkOptions);
            readBlock.LinkTo(aggregateBlock, linkOptions);
            aggregateBlock.LinkTo(joinBlock.Target2, linkOptions);
            joinBlock.LinkTo(deleteTempFileBlock, linkOptions);
            deleteTempFileBlock.LinkTo(saveBlock, linkOptions);
            saveBlock.LinkTo(raiseNotificationBlock, linkOptions);

            startBlock = moveBlock;
            finishBlock = raiseNotificationBlock;
        }
    }
}
