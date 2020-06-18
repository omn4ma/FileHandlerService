using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace FileHandler.Domain
{
    public class FileHandlerService
    {
        private readonly AbstractLogger logger;
        private readonly int maxDegreeOfParallelism;

        public FileHandlerService(int maxDegreeOfParallelism, AbstractLogger logger)
        {
            this.maxDegreeOfParallelism = maxDegreeOfParallelism;
            this.logger = logger;
        }

        public async Task Run(string sourcesFolderPath, string destinationFolderPath, CancellationToken token)
        {
            var buffer = new BufferBlock<string>();

            var consumer = ConsumeAsync(buffer, destinationFolderPath, maxDegreeOfParallelism, token, logger);
            var producer = ProduceAsync(buffer, sourcesFolderPath, token, logger);

            await consumer;
        }
        private static async Task ProduceAsync(ITargetBlock<string> target, string pathToFolder, CancellationToken token, AbstractLogger logger)
        {
            await Task.Run(async () =>
            {
                var exsistedFiles = Directory.GetFiles(pathToFolder, "*.txt", SearchOption.AllDirectories);

                foreach (var item in exsistedFiles)
                {
                    await target.SendAsync(item);
                }

                using (FileSystemWatcher watcher = new FileSystemWatcher())
                {
                    logger.WriteLine($"Producer started listening {pathToFolder}");

                    watcher.Path = pathToFolder;
                    watcher.Filter = "*.txt";

                    watcher.Created += async (sender, e) =>
                    {
                        await UglyWaitUntilTheFileIsFullyCreated(e.FullPath);

                        target.Post(e.FullPath);
                    };

                    watcher.EnableRaisingEvents = true;


                    while (!token.IsCancellationRequested)
                    {

                    };

                    logger.WriteLine($"Producer has stopped listening {pathToFolder}");

                    target.Complete();

                }
            });
        }

        /// <remarks>ToDo: migrate producer to another API</remarks>
        private static async Task UglyWaitUntilTheFileIsFullyCreated(string pathToFile)
        {
            bool fileIsBusy = true;

            while (fileIsBusy)
            {
                try
                {
                    using (var file = File.Open(pathToFile, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {

                    }

                    fileIsBusy = false;
                }
                catch (IOException)
                {
                    await Task.Delay(500);
                }
            }

        }

        private static async Task ConsumeAsync(ISourceBlock<string> source, string pathToFolder, int maxDegreeOfParallelism, CancellationToken token, AbstractLogger logger)
        {
            await Task.Run(async () =>
            {
                var documentPipeline = new DocumentsPipeline(AppDomain.CurrentDomain.BaseDirectory, pathToFolder, maxDegreeOfParallelism, token);

                documentPipeline.OnFinished += (s) => logger.WriteLine($"Сonsumer finished processing {s}");

                logger.WriteLine("Consumer is ready");


                while (await source.OutputAvailableAsync())
                {
                    var newFile = await source.ReceiveAsync();

                    await documentPipeline.Processing(newFile);
                    logger.WriteLine($"Consumer started processing {newFile}");

                }

                logger.WriteLine("Buffer is not available");

                await documentPipeline.Complete();
                logger.WriteLine("Consumer was stopped");

            });
        }
    }
}
