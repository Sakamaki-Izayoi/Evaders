namespace Evaders.Client
{
    using Core.Utility;

    internal sealed class SilentLogger : ILogger
    {
        public void Write(string text, Severity severity = Severity.Info)
        {
        }
    }
}