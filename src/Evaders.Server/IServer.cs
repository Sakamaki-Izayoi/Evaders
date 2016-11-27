namespace Evaders.Server
{
    using System;
    using System.Collections.Generic;
    using CommonNetworking.CommonPayloads;

    internal interface IServer
    {
        bool WouldAuthCollide(Guid login, IServerUser connectingUser, out IServerUser existingUser);
        long GenerateUniqueUserIdentifier();
        void HandleUserAction(IServerUser @from, LiveGameAction action);
        void HandleUserEndTurn(IServerUser user, long gameIdentifier);
        void HandleUserEnterQueue(IServerUser user, int count);
        void HandleUserLeaveQueue(IServerUser user, int count);
        void HandleUserReconnect(IServerUser user);
        void HandleUserResync(IServerUser user, long gameIdentifier);
        void Kick(IServerUser user);
        string GetMotd();
        void HandleGameEnded(ServerGame serverGame);
        void HandleGameEndedTurn(ServerGame serverGame);
    }
}