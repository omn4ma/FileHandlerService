using System;
using System.Threading;

namespace FileHandler.Application
{
    class Program
    {
        static void Main(string[] args)
        {
            ValidateArguments(args, out string sourcesFolderPath, out string destinationFolderPath);

            using (var tokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(20)))
            {
                var service = new Domain.FileHandlerService(4, new ConsoleLogger());

                service.Run(sourcesFolderPath, destinationFolderPath, tokenSource.Token).Wait();
            }
        }

        private static void ValidateArguments(string[] args, out string sourcesFolderPath, out string destinationFolderPath)
        {
            if (args.Length != 2)
            {
                throw new ArgumentException("Expected two arguments: source file path and result file path");
            }

            sourcesFolderPath = args[0];
            destinationFolderPath = args[1];
        }

        
    }
}
