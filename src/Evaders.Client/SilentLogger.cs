namespace Evaders.Client
{
    using System;
    using System.Security.Cryptography;
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
            return true;
        }

        /// <inheritdoc />
        public IDisposable BeginScope<TState>(TState state)
        {
            throw new NotSupportedException();
        }
    }
}