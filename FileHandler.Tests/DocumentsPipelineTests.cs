using FileHandler.Domain;
using FileHandler.Tests.Properties;
using NUnit.Framework;
using System;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Tests
{
    public class DocumentsPipelineTests
    {
        string sourceFolderPath = "D:\\test\\sources";
        string processingFolderPath = "D:\\test\\processing";
        string resultFolderPath = "D:\\test\\result";

        [TearDown]
        public void Cleanup()
        {
            Directory.Delete(sourceFolderPath, true);
            Directory.Delete(resultFolderPath, true);
        }

        [SetUp]
        public void Setup()
        {
            Directory.CreateDirectory(sourceFolderPath);
            Directory.CreateDirectory(processingFolderPath);
            Directory.CreateDirectory(resultFolderPath);
        }

        [TestCase("asd weD  xx'zZ", 10)]
        [TestCase("\thello", 5)]
        [TestCase("οὐλομένην", 9)]
        [TestCase("", 0)]
        [TestCase(null, 0)]
        public async Task ProcessingSingle_CheckResult(string data, int expected)
        {
            var path = CreateFileForTest(sourceFolderPath, data);
            var fileHandler = new DocumentsPipeline(processingFolderPath, resultFolderPath, 1, CancellationToken.None);

            await fileHandler.Processing(path);
            await fileHandler.Complete();

            var result = ExtractResult(resultFolderPath, Path.GetFileName(path));

            Assert.AreEqual(expected, result);
        }

        [TestCase(10, 37775580, 1)]
        [TestCase(10, 37775580, 4)]
        public async Task ProcessingArray_CheckResult(int count, int expected, int maxDegreeOfParallelism)
        {
            var paths = Enumerable.Range(1, count).Select(s => CreateFileForTest(sourceFolderPath, Resources.Example)).ToArray();
            var fileHandler = new DocumentsPipeline(processingFolderPath, resultFolderPath, maxDegreeOfParallelism, CancellationToken.None);

            foreach (var path in paths)
            {
                await fileHandler.Processing(path);
            }

            await fileHandler.Complete();
            var result = paths.Select(s => ExtractResult(resultFolderPath, Path.GetFileName(s))).Sum();

            Assert.AreEqual(expected, result);
        }

        private int ExtractResult(string resultFolderPath, string fileName)
        {
            return int.Parse(File.ReadAllText(Path.Combine(resultFolderPath, fileName)));
        }

        private string CreateFileForTest(string sourceFolderPath, string data)
        {
            var fileFullName = Path.Combine(sourceFolderPath, Guid.NewGuid().ToString() + ".txt");
            File.WriteAllText(fileFullName, data);
            return fileFullName;
        }
    }   
}