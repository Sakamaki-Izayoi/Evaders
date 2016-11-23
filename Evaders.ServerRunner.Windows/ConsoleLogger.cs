namespace Evaders.ServerRunner.Windows
{
    using System;
    using Core.Utility;

    internal class ConsoleLogger : ILogger
    {
        private readonly Severity _minSeverity;

        public ConsoleLogger(Severity minSeverity = Severity.Trace)
        {
            _minSeverity = minSeverity;
        }

        public void Write(string text, Severity severity = Severity.Info)
        {
            if (severity < _minSeverity)
                return;
            Console.WriteLine($"[{severity}] {text}");
        }
    }
}