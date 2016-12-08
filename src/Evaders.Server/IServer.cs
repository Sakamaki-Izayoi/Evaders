namespace Evaders.Server
{
    using CommonNetworking.CommonPayloads;

    public interface IServer
    {
        string Motd { get; }
        string[] GameModes { get; }

        long GenerateUniqueUserIdentifier();
        void HandleUserEnterQueue(IServerUser user, QueueAction action);
        void HandleUserLeaveQueue(IServerUser user);
        bool IsUserQueued(IServerUser user);
        void Kick(IServerUser user);
        void HandleGameEnded(ServerGame serverGame);
    }
}