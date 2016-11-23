namespace Evaders.Server
{
    using System;

    public interface IMatchmaking
    {
        event EventHandler<Matchmaking.MatchCreatedArgs> OnSuggested;

        /// <returns>How often this user is in that queue</returns>
        int GetRegisterCount(IServerUser user);

        void EnterQueue(IServerUser user);

        void LeaveQueue(IServerUser user);

        void LeaveQueueCompletely(IServerUser user);

        void Update();
    }
}