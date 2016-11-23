namespace Evaders.Client
{
    using System;

    public class MessageEventArgs : EventArgs
    {
        public readonly string Message;

        public MessageEventArgs(string message)
        {
            Message = message;
        }

        public override string ToString()
        {
            return Message;
        }
    }
}