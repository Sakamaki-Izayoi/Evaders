namespace Evaders.Server
{
    using System;
    using Integration;

    public interface IMatchmaking
    {
        event EventHandler<Matchmaking.MatchCreatedArgs> OnSuggested;

        IServerSupervisor Supervisor { get; set; }

        /// <returns>How often this user is in that queue</returns>
        bool HasUser(IServerUser user);

        void EnterQueue(IServerUser user);

        void LeaveQueue(IServerUser user);

        void LeaveQueueCompletely(IServerUser user);

        void Update();
    }
}