namespace Evaders.Core.Utility
{
    public enum Severity
    {
        Trace,
        Debug,
        Info,
        Warning,
        Error
    }

    public interface ILogger
    {
        void Write(string text, Severity severity);
    }
}