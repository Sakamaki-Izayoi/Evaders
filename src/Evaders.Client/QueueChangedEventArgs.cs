namespace Evaders.Client
{
    using System;
    using CommonNetworking.CommonPayloads;

    public class QueueChangedEventArgs : EventArgs
    {
        public readonly string GameMode;
        public readonly int Count;

        public QueueChangedEventArgs(QueueState state)
        {
            GameMode = state.GameMode;
            Count = state.Count;
        }
    }
}