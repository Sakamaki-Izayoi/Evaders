namespace Evaders.Client
{
    using System;
    using CommonNetworking.CommonPayloads;

    public interface IQueuer
    {
        event EventHandler<QueueChangedEventArgs> OnUserStateChanged;
        event EventHandler<GameEventArgs> OnGameStarted;

        UserState LastState { get; }

        void EnterQueue(string mode);
        void LeaveQueue();
    }
}