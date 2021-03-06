﻿namespace Evaders.Server
{
    using System;
    using CommonNetworking.CommonPayloads;

    internal interface IServer
    {
        bool WouldAuthCollide(Guid login, IServerUser connectingUser, out IServerUser existingUser);
        long GenerateUniqueUserIdentifier();
        void HandleUserAction(IServerUser from, LiveGameAction action);
        void HandleUserEndTurn(IServerUser user, long gameIdentifier);
        void HandleUserEnterQueue(IServerUser user, QueueAction count);
        void HandleUserLeaveQueue(IServerUser user, QueueAction count);
        void HandleUserReconnect(IServerUser user);
        void HandleUserResync(IServerUser user, long gameIdentifier);
        void Kick(IServerUser user);
        string GetMotd();
        void HandleGameEnded(ServerGame serverGame);
        string[] GetGameModes();
    }
}