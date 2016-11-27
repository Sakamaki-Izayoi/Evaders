namespace Evaders.Client
{
    using System;
    using Microsoft.Extensions.Logging;

    public class SilentLogger : ILogger
    {
        /// <inheritdoc />
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
        }

        /// <inheritdoc />
        public bool IsEnabled(LogLevel logLevel)
        {
            return false;
        }

        /// <inheritdoc />
        public IDisposable BeginScope<TState>(TState state)
        {
            throw new NotSupportedException();
        }
    }
}