namespace Evaders.Client
{
    using System;
    using CommonNetworking.CommonPayloads;

    public class QueueChangedEventArgs : EventArgs
    {
        public readonly UserState State;

        public QueueChangedEventArgs(UserState state)
        {
            State = state;
        }
    }
}