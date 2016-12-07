namespace Evaders.Client
{
    using System;
    using System.IO;
    using System.Net;
    using System.Net.Sockets;
    using System.Text;
    using CommonNetworking;
    using CommonNetworking.CommonPayloads;
    using Core.Game;
    using Microsoft.Extensions.Logging;
    using Payloads;

    public class Connection : IQueuer
    {
        public event EventHandler<QueueChangedEventArgs> OnUserStateChanged;
        public event EventHandler<GameEventArgs> OnGameStarted;

        public event EventHandler<LoggedInEventArgs> OnLoggedIn;
        //public event EventHandler<MessageEventArgs> OnKicked;
        public event EventHandler<MessageEventArgs> OnIllegalAction;
        public ClientGame Game { get; private set; }
        public UserState LastState { get; private set; }
        private readonly EasySocket _easySocket;
        private readonly ILogger _logger;
        private PacketParser<PacketS2C> _packetParser;

        public Connection(Guid identifier, string displayName, IPAddress serverAddr, ushort serverPort, PacketParser<PacketS2C> parser, ILogger logger)
        {
            _logger = logger;
            var client = new Socket(SocketType.Stream, ProtocolType.Tcp);
            client.Connect(serverAddr, serverPort);
            _easySocket = new EasySocket(client);
            Startup(identifier, displayName, parser);
        }

        public Connection(Guid identifier, string displayName, Socket connectedSocket, PacketParser<PacketS2C> parser, ILogger logger)
        {
            if (!connectedSocket.Connected)
                throw new ArgumentException("Not connected", nameof(connectedSocket));
            _logger = logger;
            _easySocket = new EasySocket(connectedSocket);
            Startup(identifier, displayName, parser);
        }

        void IQueuer.EnterQueue(string gameMode)
        {
            Send(Packet.PacketTypeC2S.EnterQueue, new QueueAction(gameMode));
        }

        void IQueuer.LeaveQueue()
        {
            Send(Packet.PacketTypeC2S.LeaveQueue);
        }

        public static Connection ConnectJson(Guid identifier, string displayName, IPAddress serverAddr, ushort serverPort, ILogger logger)
        {
            return new Connection(identifier, displayName, serverAddr, serverPort, new PacketParserJson<PacketS2C>(logger, Encoding.Unicode), logger);
        }

        public static Connection ConnectBson(Guid identifier, string displayName, IPAddress serverAddr, ushort serverPort, ILogger logger)
        {
            return new Connection(identifier, displayName, serverAddr, serverPort, new PacketParserBson<PacketS2C>(logger), logger);
        }

        private void Startup(Guid identifier, string displayName, PacketParser<PacketS2C> packetParser)
        {
            var parser = packetParser;
            _easySocket.StartJobs(EasySocket.SocketTasks.Receive);
            _easySocket.OnReceived += (sender, args) =>
            {
                parser.Continue(args);
                _easySocket.GiveBack(args.Buffer);
            };
            parser.OnReceived += OnReceived;
            _packetParser = parser;
            _logger.LogDebug("Socket set up");
            Send(Packet.PacketTypeC2S.Authorize, new Authorize(identifier, displayName));
            _logger.LogTrace("Authorization request sent");
        }

        public void Update()
        {
            _easySocket.Work();
        }

        private void OnReceived(PacketS2C packet)
        {
            _logger.LogTrace($"Received packet: {packet}");
            switch (packet.Type)
            {
                case Packet.PacketTypeS2C.AuthResult:
                {
                    var state = packet.GetPayload<AuthCompleted>();
                    OnLoggedIn?.Invoke(this, new LoggedInEventArgs(this, state.Motd, state.GameModes));
                }
                    break;
                case Packet.PacketTypeS2C.ConfirmedGameAction:
                {
                    var gameAction = packet.GetPayload<GameAction>();
                    if (Game == null)
                    {
                        _logger.LogError($"Action in unknown game: {gameAction}");
                        Send(Packet.PacketTypeC2S.ForceResync);
                        return;
                    }

                    var ownerOfEntity = Game.GetOwnerOfEntity(gameAction.ControlledEntityIdentifier);
                    if (ownerOfEntity == null)
                    {
                        _logger.LogError($"Corrupted game state - cannot find entity: {gameAction.ControlledEntityIdentifier} in game");
                        Send(Packet.PacketTypeC2S.ForceResync);
                    }
                    else
                        Game.AddActionWithoutNetworking(ownerOfEntity, gameAction);
                }
                    break;
                case Packet.PacketTypeS2C.IllegalAction:
                {
                    var illegalAction = packet.GetPayload<IllegalAction>();
                    OnIllegalAction?.Invoke(this, new MessageEventArgs(illegalAction.Message));

                    if ((Game == null) && illegalAction.InsideGame)
                    {
                        _logger.LogError("Server claims illegal action in game - but there is no active game");
                        Send(Packet.PacketTypeC2S.ForceResync);
                    }
                }
                    break;
                case Packet.PacketTypeS2C.NextTurn:
                {
                    if (Game != null)
                    {
                        Game.DoNextTurn();
                    }
                    else
                    {
                        _logger.LogError($"Server sent turn end - but there is no active game");
                        Send(Packet.PacketTypeC2S.ForceResync);
                    }
                }
                    break;
                case Packet.PacketTypeS2C.GameState:
                {
                    var state = packet.GetPayload<GameState>();
                    Game = state.State;
                    state.State.SetGameDetails(state.YourIdentifier, state.GameIdentifier, this);
                    Game.RequestClientActions();
                    OnGameStarted?.Invoke(this, new GameEventArgs(Game));
                }
                    break;
                case Packet.PacketTypeS2C.GameEnd:
                {
                    var end = packet.GetPayload<GameEnd>(); // Todo


                    Game = null;
                }
                    break;
                case Packet.PacketTypeS2C.UserState:
                    LastState = packet.GetPayload<UserState>();
                    OnUserStateChanged?.Invoke(this, new QueueChangedEventArgs(LastState));
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        internal void Send(Packet.PacketTypeC2S type, object payloadData = null)
        {
            var packetC2S = new PacketC2S(type, payloadData);
            _logger.LogTrace($"Sending packet: {packetC2S}");
            var payload = _packetParser.FromPacket(packetC2S);
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