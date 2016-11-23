namespace Evaders.Client
{
    using System;

    public interface IQueuer
    {
        event EventHandler<CountChangedEventArgs> OnServersideQueueCountChanged;
        event EventHandler<GameEventArgs> OnJoinedGame;
        event EventHandler<GameEventArgs> OnLeftGame;
        int CurrentlyRunningGames { get; }
        int LastServersideQueueCount { get; }

        void EnterQueue(int count = 1);
        void LeaveQueue(int count = 1);
    }
}