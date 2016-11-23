namespace Evaders.Server
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Sockets;
    using CommonNetworking;
    using Core.Game;

    public interface IServerUser : IUser, IDisposable
    {
        string Username { get; }
        bool IsBot { get; }
        bool FullGameState { get; }
        Guid Login { get; }
        bool Authorized { get; }
        IPAddress Address { get; }
        bool Disposed { get; }

        void IllegalAction(string reason);
        void Inherit(IServerUser other, Socket socket);
        void Send(Packet.PacketTypeS2C type, object payload);
        void Update();
    }
}