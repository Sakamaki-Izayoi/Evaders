namespace ExampleBot
{
    using System;
    using Evaders.Core.Utility;

    internal class ConsoleLogger : ILogger
    {
        public void Write(string text, Severity severity = Severity.Info)
        {
            Console.WriteLine($"[{severity}] {text}");
        }
    }
}