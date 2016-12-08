namespace Evaders.Server
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using Integration;

    public interface IMatchmaking
    {
        void Configure(IMatchmakingServer server, IServerSupervisor supervisor, string gameMode, double maxTimeInQueueSec);

        /// <returns>How often this user is in that queue</returns>
        bool HasUser(IServerUser user);

        void EnterQueue(IServerUser user);

        void LeaveQueue(IServerUser user);

        IEnumerable<IServerUser> RemoveAll();

        void Update();
    }
}