namespace Evaders.Client
{
    using System;

    public class LoggedInEventArgs : EventArgs
    {
        public readonly string Message;
        public readonly IQueuer Queuer;

        public LoggedInEventArgs(IQueuer queuer, string message)
        {
            Queuer = queuer;
            Message = message;
        }
    }
}