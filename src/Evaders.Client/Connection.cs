namespace Evaders.Client
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Sockets;
    using System.Text;
    using CommonNetworking;
    using CommonNetworking.CommonPayloads;
    using Core.Game;
    using Core.Utility;
    using Microsoft.Extensions.Logging;
    using Payloads;

    public class Connection : IQueuer, IGameProvider
    {
        event EventHandler<CountChangedEventArgs> IQueuer.OnServersideQueueCountChanged
        {
            add
            {
                OnServersideQueueCountChangedInternal += value;
            }
            remove
            {
                OnServersideQueueCountChangedInternal -= value;
            }
        }

        event EventHandler<GameEventArgs> IQueuer.OnJoinedGame
        {
            add
            {
                OnJoinedGameInternal += value;
            }
            remove
            {
                OnJoinedGameInternal -= value;
            }
        }

        event EventHandler<GameEventArgs> IQueuer.OnLeftGame
        {
            add
            {
                OnLeftGameInternal += value;
            }
            remove
            {
                OnLeftGameInternal -= value;
            }
        }

        private event EventHandler<CountChangedEventArgs> OnServersideQueueCountChangedInternal;
        private event EventHandler<GameEventArgs> OnJoinedGameInternal;
        private event EventHandler<GameEventArgs> OnLeftGameInternal;

        public event EventHandler<LoggedInEventArgs> OnLoggedIn;
        //public event EventHandler<MessageEventArgs> OnKicked;
        public event EventHandler<MessageEventArgs> OnIllegalAction;

        public IReadOnlyDictionary<long, GameBase> RunningGames => _games.ToDictionary(item => item.Key, item => (GameBase)item.Value);
        public int CurrentlyRunningGames => _games.Count;
        private readonly EasySocket _easySocket;
        private readonly Dictionary<long, ClientGame> _games = new Dictionary<long, ClientGame>();
        private readonly ILogger _logger;
        private int _lastQueueCount;

        public Connection(Guid identifier, string displayName, IPAddress serverAddr, ushort serverPort, ILogger logger)
        {
            _logger = logger;
            var client = new Socket(SocketType.Stream, ProtocolType.Tcp);
            client.Connect(serverAddr, serverPort);
            _easySocket = new EasySocket(client);
            Startup(identifier, displayName);
        }

        public Connection(Guid identifier, string displayName, Socket connectedSocket, ILogger logger)
        {
            if (!connectedSocket.Connected)
                throw new ArgumentException("Not connected", nameof(connectedSocket));
            _logger = logger;
            _easySocket = new EasySocket(connectedSocket);
            Startup(identifier, displayName);
        }

        public Connection(Guid identifier, string displayName, IPAddress serverAddr, ushort serverPort) : this(identifier, displayName, serverAddr, serverPort, new SilentLogger())
        {
        }

        public Connection(Guid identifier, string displayName, Socket connectedSocket) : this(identifier, displayName, connectedSocket, new SilentLogger())
        {
        }

        void IQueuer.EnterQueue(string gameMode, int count)
        {
            Send(Packet.PacketTypeC2S.EnterQueue, new QueueAction(gameMode, count));
        }

        void IQueuer.LeaveQueue(string gameMode, int count)
        {
            Send(Packet.PacketTypeC2S.LeaveQueue, new QueueAction(gameMode, count));
        }

        private void Startup(Guid identifier, string displayName)
        {
            var parser = new PacketParser(_logger, Encoding.Unicode);
            _easySocket.StartJobs(EasySocket.SocketTasks.Receive);
            _easySocket.OnReceived += (sender, args) =>
            {
                parser.Continue(args);
                _easySocket.GiveBack(args.Buffer);
            };
            parser.OnReceivedJson += OnReceived;
            _logger.LogDebug("Socket set up");
            Send(Packet.PacketTypeC2S.Authorize, new Authorize(identifier, displayName));
            _logger.LogTrace("Authorization request sent");
        }

        public void Update()
        {
            _easySocket.Work();
        }

        private void OnReceived(string json)
        {
            var packet = JsonNet.Deserialize<PacketS2C>(json);
            _logger.LogTrace($"Received packet: {packet}");
            switch (packet.Type)
            {
                case Packet.PacketTypeS2C.AuthResult:
                    {
                        var state = packet.GetPayload<AuthCompleted>();
                        OnLoggedIn?.Invoke(this, new LoggedInEventArgs(this, state.Motd, state.GameModes));
                    }
                    break;
                case Packet.PacketTypeS2C.GameAction:
                    {
                        var gameAction = packet.GetPayload<LiveGameAction>();
                        if (_games.ContainsKey(gameAction.GameIdentifier))
                        {
                            var game = _games[gameAction.GameIdentifier];
                            var ownerOfEntity = game.GetOwnerOfEntity(gameAction.ControlledEntityIdentifier);
                            if (ownerOfEntity == null)
                            {
                                _logger.LogError($"Corrupted game state - cannot find entity: {gameAction.ControlledEntityIdentifier} in game {gameAction.GameIdentifier}");
                                Send(Packet.PacketTypeC2S.ForceResync, gameAction.GameIdentifier);
                            }
                            else
                                _games[gameAction.GameIdentifier].AddActionWithoutNetworking(ownerOfEntity, gameAction);
                        }
                        else
                        {
                            _logger.LogError($"Action in unknown game: {gameAction.GameIdentifier}");
                            Send(Packet.PacketTypeC2S.ForceResync, gameAction.GameIdentifier);
                        }
                    }
                    break;
                case Packet.PacketTypeS2C.IllegalAction:
                    {
                        var illegalAction = packet.GetPayload<IllegalAction>();
                        if (!illegalAction.InsideGame)
                            OnIllegalAction?.Invoke(this, new MessageEventArgs(illegalAction.Message));
                        else if (illegalAction.GameIdentifier != null && _games.ContainsKey(illegalAction.GameIdentifier.Value))
                            _games[illegalAction.GameIdentifier.Value].HandleServerIllegalAction(illegalAction.Message);
                        else if (illegalAction.GameIdentifier != null)
                        {
                            _logger.LogError($"Server refused action in unknown game: {illegalAction.GameIdentifier}");
                            Send(Packet.PacketTypeC2S.ForceResync, illegalAction.GameIdentifier);
                        }
                        else
                            _logger.LogWarning("Cannot handle server packet (Claims illegal action in game, but does not specify the game identifier)");
                    }
                    break;
                case Packet.PacketTypeS2C.NextRound:
                    {
                        var gameIdentifier = packet.GetPayload<long>();
                        if (_games.ContainsKey(gameIdentifier))
                            _games[gameIdentifier].DoNextTurn();
                        else
                        {
                            _logger.LogError($"Server sent turn end in unknown game: {gameIdentifier}");
                            Send(Packet.PacketTypeC2S.ForceResync, gameIdentifier);
                        }
                    }
                    break;
                case Packet.PacketTypeS2C.GameState:
                    {
                        var state = packet.GetPayload<GameState>();
                        _games[state.GameIdentifier] = state.State;
                        state.State.SetGameDetails(state.YourIdentifier, state.GameIdentifier, this);
                        OnJoinedGameInternal?.Invoke(this, new GameEventArgs(state.State));
                        state.State.RequestClientActions();
                    }
                    break;
                case Packet.PacketTypeS2C.GameEnd:
                    {
                        var end = packet.GetPayload<GameEnd>();
                        if (_games.ContainsKey(end.GameIdentifier))
                        {
                            var game = _games[end.GameIdentifier];
                            _games.Remove(end.GameIdentifier);
                            OnLeftGameInternal?.Invoke(this, new GameEventArgs(game));
                        }
                    }
                    break;
                case Packet.PacketTypeS2C.QueueState:
                    var args = new CountChangedEventArgs(packet.GetPayload<int>());
                    _lastQueueCount = args.Count;
                    OnServersideQueueCountChangedInternal?.Invoke(this, args);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        internal void Send(Packet.PacketTypeC2S type, object payloadData)
        {
            var packetC2S = new PacketC2S(type, payloadData);
            _logger.LogTrace($"Sending packet: {packetC2S}");
            var payload = Encoding.Unicode.GetBytes(JsonNet.Serialize(packetC2S));
            using (var memStream = new MemoryStream())
            {
                var writer = new BinaryWriter(memStream);
                writer.Write(payload.Length);
                writer.Write(payload);
                _easySocket.SendAsync(memStream.ToArray());
            }
        }
    }
}