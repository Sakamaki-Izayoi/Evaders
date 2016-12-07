namespace Evaders.Server
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using CommonNetworking;
    using CommonNetworking.CommonPayloads;
    using Core.Game;
    using JetBrains.Annotations;
    using Microsoft.Extensions.Logging;
    using Newtonsoft.Json;

    internal class User : IServerUser
    {
        public bool Connected => _socket.Connected;
        public IPAddress Address => (_socket.Socket.RemoteEndPoint as IPEndPoint)?.Address ?? IPAddress.Any;

        [JsonProperty]
        public long Identifier { get; private set; }

        public Guid Login { get; private set; }

        [JsonProperty]
        public string Username { get; private set; }

        [JsonProperty]
        public bool IsPassiveBot { get; private set; }

        public bool IsIngame => _myGame != null;
        public bool Authorized { get; private set; }
        public bool FullGameState { get; private set; } // Send full game state each turn or just differences

        private readonly object _authLock = new object();
        private readonly ILogger _logger;
        private readonly IRulesProvider _rules;
        private readonly IServer _server;

        private readonly PacketParser<PacketC2S> _packetParser;
        private EasyTaskSocket _socket;
        private ServerGame _myGame;

        public User(Socket socket, ILogger logger, IServer server, IRulesProvider rules, PacketParser<PacketC2S> packetParser)
        {
            _logger = logger;
            _server = server;
            _rules = rules;
            _packetParser = packetParser;
            _packetParser.OnReceived += HandlePacket;
            SetupSocket(socket);
        }

        public void IllegalAction(string reason)
        {
            Send(Packet.PacketTypeS2C.IllegalAction, new IllegalAction(reason, false, null));
        }

        ///// <summary>
        /////     Replaces the socket with the socket of the given user. This should be used to let a reconnecting user continue
        /////     where he left
        ///// </summary>
        ///// <param name="user"></param>
        ///// <param name="socket"></param>
        //public void Inherit(IServerUser user, Socket socket)
        //{
        //    var userImpl = user as User;

        //    if (userImpl == null)
        //        throw new NotImplementedException(); // assuming there are no other implementations of IServerUser than User

        //    _socket.Dispose(); // Dispose disconnected
        //    userImpl._socket.StopJobs(); // Discontinue wrapper because the events will call the methods of the new (duplicate) user
        //    SetupSocket(userImpl._socket.Socket); // Recreate wrapper
        //    _packetParser = userImpl._packetParser;
        //    Username = user.Username;
        //    FullGameState = user.FullGameState;
        //    Authorized = user.Authorized;
        //}

        public void Send(Packet.PacketTypeS2C type, object payload)
        {
            Send(new PacketS2C(type, payload));
        }

        public void SetIngame([CanBeNull] ServerGame game)
        {
            _myGame = game;
            Send(Packet.PacketTypeS2C.UserState, new UserState(_server.IsUserQueued(this), IsIngame, IsPassiveBot, Username, FullGameState));
        }

        //public void OnGameStarted()
        //{
        //    lock (_authLock)
        //        GameCount++;
        //}

        //public void OnGameEnded()
        //{
        //    lock (_authLock)
        //        GameCount--;
        //}

        public void Dispose()
        {
            Authorized = false;
            _socket.Dispose();
        }

        private void SetupSocket(Socket socket)
        {
            _socket = new EasyTaskSocket(socket);
            _socket.StartJobs(EasyTaskSocket.SocketTasks.Receive);
            _socket.OnSent += OnSent;
            _socket.OnReceived += (sender, args) =>
            {
                _packetParser.Continue(args);
                _socket.GiveBack(args.Buffer);
            };
        }

        private void Send(Packet packetS2C)
        {
            if (!Connected || _socket.Stopped)
            {
                _server.Kick(this);
                return;
            }

            var payload = _packetParser.FromPacket(packetS2C);
            using (var memStream = new MemoryStream())
            {
                var writer = new BinaryWriter(memStream);
                writer.Write(payload.Length);
                writer.Write(payload);
                _socket.SendAsync(memStream.ToArray());
            }
        }

        private void OnSent(object sender, SocketAsyncEventArgs socketAsyncEventArgs)
        {
            if (socketAsyncEventArgs.BytesTransferred != socketAsyncEventArgs.Count)
            {
                _logger.LogInformation($"{this} disconnected");
                _server.Kick(this);
            }
        }

        private void HandlePacket(PacketC2S packet)
        {
            if (!Authorized && (packet.Type != Packet.PacketTypeC2S.Authorize))
            {
                _logger.LogDebug($"{this} tried to send unauthorized packets!");
                IllegalAction("Please authorize first!");
                return;
            }
            _logger.LogDebug($"{this} sent packet: {packet.Type}");

            switch ((Packet.PacketTypeC2S)packet.TypeNum)
            {
                case Packet.PacketTypeC2S.Authorize:
                    lock (_authLock)
                    {
                        if (Authorized)
                        {
                            IllegalAction("Already authorized");
                            return;
                        }
                        var authorize = packet.GetPayload<Authorize>();
                        Login = authorize.Identifier;
                        Username = authorize.Name;
                        FullGameState = authorize.FullGameState;

                        if (Username.Length > _rules.MaxUsernameLength)
                        {
                            IllegalAction("Are you Daenerys Targaryen? No? Then you can't possibly have a name that long. (Exceeded name length limitation)");
                            return;
                        }

                        if ((Login == null) || (Login.ToByteArray().Distinct().Count() <= 1))
                        {
                            IllegalAction("Invalid login. The login is supposed to be a GUID of your choice (choose any, but keep that one!). It needs to be in a notation that can be parsed by this: https://msdn.microsoft.com/en-us/library/system.guid.parse(v=vs.110).aspx");
                            return;
                        }

                        if (string.IsNullOrWhiteSpace(Username))
                        {
                            IllegalAction("Now, don't get me wrong, I really like your name. Reminds me of 'No Game No Life'. However, the spectator client will be really sad if he can't render anything, so please be a little more creative and come back with something not-empty!");
                            return;
                        }

                        Identifier = _server.GenerateUniqueUserIdentifier();
                        Authorized = true;

                        //IServerUser existingConnection;

                        //if (_server.WouldAuthCollide(Login, this, out existingConnection))
                        //{
                        //    _logger.LogInformation($"User relogging: {this} -> {existingConnection}");
                        //    if (existingConnection.Authorized && existingConnection.Connected && !Address.Equals(existingConnection.Address))
                        //        _logger.LogWarning($"Possible account sharing: {this} and {existingConnection}");

                        //    existingConnection.Inherit(this, _socket.Socket);
                        //    Authorized = false;
                        //    _server.Kick(this);
                        //    _server.HandleUserReconnect(existingConnection);
                        //    return;
                        //}
                        Send(Packet.PacketTypeS2C.AuthResult, new AuthCompleted(_server.Motd, _server.GameModes, _server.MaxQueueCount));
                    }
                    break;
                case Packet.PacketTypeC2S.GameAction:
                    if (!IsIngame)
                    {
                        IllegalAction("You are not in a game. Cannot execute game action.");
                        return;
                    }

                    _myGame.AddGameAction(this, packet.GetPayload<GameAction>());
                    break;
                case Packet.PacketTypeC2S.SwitchQueueMode:
                    IsPassiveBot = !IsPassiveBot;
                    _logger.LogInformation($"{this} is now a passive bot: {IsPassiveBot}");
                    break;
                case Packet.PacketTypeC2S.EnterQueue:
                    _server.HandleUserEnterQueue(this, packet.GetPayload<QueueAction>());
                    break;
                case Packet.PacketTypeC2S.LeaveQueue:
                    _server.HandleUserLeaveQueue(this);
                    break;
                case Packet.PacketTypeC2S.TurnEnd:
                    if (!IsIngame)
                    {
                        IllegalAction("You are not in a game. Cannot end turn");
                        return;
                    }

                    _myGame.UserRequestsEndTurn(this);
                    break;
                case Packet.PacketTypeC2S.ForceResync:
                    _myGame.HandleReconnect(this);
                    break;
                case Packet.PacketTypeC2S.GetUserState:
                    Send(Packet.PacketTypeS2C.UserState, new UserState(_server.IsUserQueued(this), IsIngame, IsPassiveBot, Username, FullGameState));
                    break;
                default:
                    _logger.LogWarning($"Authorized user sent invalid packet type: {packet.TypeNum} from: {this}");
                    IllegalAction("Unknown packet type");
                    break;
            }
        }

        public override string ToString()
        {
            return $"{Username}@{Address} ({Login}/{Identifier})";
        }
    }
}