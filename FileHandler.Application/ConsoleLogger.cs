using FileHandler.Domain;
using System;
using System.Diagnostics;

namespace FileHandler.Application
{
    public class ConsoleLogger : AbstractLogger
    {
        Stopwatch timer = Stopwatch.StartNew();

        public override void WriteLine(string message)
        {
            Console.WriteLine($"[{timer.Elapsed.Minutes}:{timer.Elapsed.Seconds.ToString("00")}:{timer.Elapsed.Milliseconds}] {message}");
        }
    }
}
