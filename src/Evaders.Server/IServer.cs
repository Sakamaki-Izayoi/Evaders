namespace Evaders.Server
{
    using System;
    using CommonNetworking.CommonPayloads;
    using Core.Game;

    public interface IServer
    {
        string Motd { get; }
        int MaxQueueCount { get; }
        string[] GameModes { get; }
        
        long GenerateUniqueUserIdentifier();
        void HandleUserEnterQueue(IServerUser user, QueueAction action);
        void HandleUserLeaveQueue(IServerUser user);
        bool IsUserQueued(IServerUser user);
        void Kick(IServerUser user);
        void HandleGameEnded(ServerGame serverGame);
    }
}