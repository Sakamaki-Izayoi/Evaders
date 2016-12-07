namespace Evaders.Client
{
    using System;

    public interface IQueuer
    {
        event EventHandler<QueueChangedEventArgs> OnServersideQueueCountChanged;
        event EventHandler<GameEventArgs> OnJoinedGame;
        event EventHandler<GameEventArgs> OnLeftGame;
        int CurrentlyRunningGames { get; }

        void EnterQueue(string mode, int count = 1);
        void LeaveQueue(string mode, int count = 1);
    }
}