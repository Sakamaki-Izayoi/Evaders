namespace Evaders.Server
{
    using System;
    using System.Net;
    using CommonNetworking;
    using Core.Game;

    public interface IServerUser : IUser, IDisposable
    {
        string Username { get; }
        bool IsPassiveBot { get; }
        bool FullGameState { get; }
        Guid Login { get; }
        bool Authorized { get; }
        IPAddress Address { get; }
        bool IsIngame { get; }

        void IllegalAction(string reason);
        void Send(Packet.PacketTypeS2C type, object payload = null);
        void SetIngame(ServerGame game);
    }
}