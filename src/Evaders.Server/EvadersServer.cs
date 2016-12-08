namespace Evaders.Server
{
    using System;
    using System.Collections.Concurrent;
    using System.Linq;
    using System.Net;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading;
    using CommonNetworking;
    using CommonNetworking.CommonPayloads;
    using Core.Game;
    using Integration;
    using JetBrains.Annotations;
    using Microsoft.Extensions.Logging;

    public class EvadersServer : IServer, IRulesProvider, IMatchmakingServer
    {
        public string Motd => _configMotd;
        public string[] GameModes => _configGameModes;
        public int MaxUsernameLength => _configMaxUsernameLength;
        public bool ServerListening => _serverSocket.IsBound && !_serverSocket.Stopped;
        public bool BsonServerListening => _serverSocketBson.IsBound && !_serverSocketBson.Stopped;
        private readonly ConcurrentDictionary<IServerUser, DateTime> _connectedUsers = new ConcurrentDictionary<IServerUser, DateTime>();
        private readonly ILogger _logger;
        private ConcurrentDictionary<string, IMatchmaking> _matchmaking;
        private readonly ConcurrentDictionary<long, ServerGame> _runningGames = new ConcurrentDictionary<long, ServerGame>();
        private readonly IProviderFactory<GameSettings> _settingsFactory;
        private readonly IProviderFactory<IMatchmaking> _matchmakingFactory;
        private readonly IProviderFactory<IServerSupervisor> _supervisorFactory;
        private long _gameIdentifier;
        private EasyTaskSocket _serverSocket;
        private EasyTaskSocket _serverSocketBson;
        private long _userIdentifier;

        private string _configMotd;
        private string[] _configGameModes;
        private int _configMaxUsernameLength;
        private IPAddress _configIpAddress;
        private ushort _configPort;
        private ushort _configBSONPort;
        private double _configMaxTimeInQueueSec;

        private readonly object _applySettingsLock = new object();

        public EvadersServer([NotNull] IProviderFactory<IServerSupervisor> supervisorFactory, [NotNull] IProviderFactory<GameSettings> settingsFactory, [NotNull] IProviderFactory<IMatchmaking> matchmakingFactory, [NotNull] ILogger logger, [NotNull] ServerSettings config)
        {
            if (supervisorFactory == null)
                throw new ArgumentNullException(nameof(supervisorFactory));
            if (settingsFactory == null)
                throw new ArgumentNullException(nameof(settingsFactory));
            if (matchmakingFactory == null)
                throw new ArgumentNullException(nameof(matchmakingFactory));
            if (logger == null)
                throw new ArgumentNullException(nameof(logger));
            if (config == null)
                throw new ArgumentNullException(nameof(config));
            if (!config.IsValid)
                throw new ArgumentException("Invalid config", nameof(config));

            _supervisorFactory = supervisorFactory;
            _settingsFactory = settingsFactory;
            _matchmakingFactory = matchmakingFactory;
            _logger = logger;

            WriteSettings(config);
            SetupMatchmaking();
        }

        private void SetupMatchmaking()
        {
            _logger.LogInformation("Setting up matchmaking");
            lock (_applySettingsLock)
            {
                if (_matchmaking == null)
                    _matchmaking = new ConcurrentDictionary<string, IMatchmaking>(_configGameModes.ToDictionary(item => item, _matchmakingFactory.Create));
                else
                {
                    var droppedModes = _matchmaking.Where(item => !_configGameModes.Contains(item.Key)).ToArray();
                    foreach (var dropped in droppedModes)
                    {
                        var droppedUsers = dropped.Value.RemoveAll();
                        foreach (var serverUser in droppedUsers)
                        {
                            serverUser.Send(Packet.PacketTypeS2C.UserState, new UserState(false, serverUser.IsIngame, serverUser.IsPassiveBot, serverUser.Username, serverUser.FullGameState));
                        }
                    }

                    foreach (var mode in _configGameModes.Where(item => !_matchmaking.ContainsKey(item)))
                    {
                        _matchmaking.TryAdd(mode, _matchmakingFactory.Create(mode));
                    }
                }

                foreach (var keyValuePair in _matchmaking)
                    keyValuePair.Value.Configure(this, _supervisorFactory.Create(keyValuePair.Key), keyValuePair.Key, _configMaxTimeInQueueSec);
            }
        }

        private void WriteSettings(ServerSettings settings)
        {
            lock (_applySettingsLock)
            {
                _configMaxUsernameLength = settings.MaxUsernameLength;
                _configBSONPort = settings.BSONPort;
                _configPort = settings.Port;
                _configGameModes = settings.GameModes;
                _configIpAddress = settings.IP;
                _configMotd = settings.Motd;
                _configMaxTimeInQueueSec = settings.MaxTimeInQueueSec;
            }
        }

        public void ApplySettings(ServerSettings settings = null)
        {
            lock (_applySettingsLock)
            {
                if (settings != null) WriteSettings(settings);

                if (_serverSocket == null || !_configIpAddress.Equals(((IPEndPoint)_serverSocket.Socket.LocalEndPoint).Address))
                    Start();

                SetupMatchmaking();
            }
        }

        public void Start()
        {
            lock (_applySettingsLock)
            {
                if ((_serverSocket != null) || (_serverSocketBson != null))
                {
                    _serverSocket?.Dispose();
                    _serverSocketBson?.Dispose();
                }

                _logger.LogInformation("Setting up tcp accept socket");

                var listener = new TcpListener(_configIpAddress, _configPort);
                var listenerBson = new TcpListener(_configIpAddress, _configBSONPort);

                listener.Start();
                listenerBson.Start();

                _serverSocket = new EasyTaskSocket(listener.Server);
                _serverSocketBson = new EasyTaskSocket(listenerBson.Server);

                _serverSocket.OnAccepted += OnClientConnectedJson;
                _serverSocketBson.OnAccepted += OnClientConnectedBson;

                if (!_serverSocket.StartJobs(EasyTaskSocket.SocketTasks.Accept) || !_serverSocketBson.StartJobs(EasyTaskSocket.SocketTasks.Accept))
                    throw new Exception("Could not start network jobs");

                _logger.LogInformation("Server online!");
            }
        }

        public void CreateGame(string gameMode, IMatchmaking source, IServerUser[] users)
        {
            lock (_runningGames)
            {
                _gameIdentifier++;

                var game = new ServerGame(this, _supervisorFactory.Create(gameMode), users, _settingsFactory.Create(gameMode), _gameIdentifier, _logger);
                foreach (var serverUser in users)
                {
                    source.LeaveQueue(serverUser);
                    serverUser.SetIngame(game);
                    game.HandleReconnect(serverUser);
                }
                if (!_runningGames.TryAdd(_gameIdentifier, game))
                    _logger.LogError($"Cannot add game with ID: {game.GameIdentifier}, current is: {_gameIdentifier}");
            }
        }

        public long GenerateUniqueUserIdentifier()
        {
            return Interlocked.Increment(ref _userIdentifier);
        }

        public bool IsUserQueued(IServerUser user) => _matchmaking.Any(item => item.Value.HasUser(user));

        public void HandleUserEnterQueue(IServerUser user, QueueAction action)
        {
            if (user.IsIngame)
            {
                user.IllegalAction("You cannot queue while ingame");
                return;
            }

            var queued = IsUserQueued(user);
            if (queued)
            {
                user.Send(Packet.PacketTypeS2C.UserState, new UserState(true, user.IsIngame, user.IsPassiveBot, user.Username, user.FullGameState));
                return;
            }

            var matchmaking = GetMatchmaking(user, action);
            if (matchmaking == null)
            {
                user.IllegalAction("There is no such gamemode");
                return;
            }

            lock (matchmaking)
            {
                matchmaking.EnterQueue(user);
            }

            user.Send(Packet.PacketTypeS2C.UserState, new UserState(true, user.IsIngame, user.IsPassiveBot, user.Username, user.FullGameState));
        }

        public void HandleUserLeaveQueue(IServerUser user)
        {
            var matchmaking = _matchmaking.FirstOrDefault(item => item.Value.HasUser(user)).Value;

            if (matchmaking != null)
                lock (matchmaking)
                {
                    matchmaking.LeaveQueue(user);
                }

            if (user.Connected)
                user.Send(Packet.PacketTypeS2C.UserState, new UserState(false, user.IsIngame, user.IsPassiveBot, user.Username, user.FullGameState));
        }

        void IServer.Kick(IServerUser user)
        {
            DateTime connectedTime;

            if (IsUserQueued(user))
                HandleUserLeaveQueue(user);

            lock (_connectedUsers)
            {
                _connectedUsers.TryRemove(user, out connectedTime);
            }
            if (user.Connected)
                user.Dispose();

            _logger.LogDebug($"Kicking user: {user}, who connected {connectedTime}");
        }

        void IServer.HandleGameEnded(ServerGame serverGame)
        {
            ServerGame game;
            _runningGames.TryRemove(serverGame.GameIdentifier, out game);
        }

        public bool WouldAuthCollide(Guid login, IServerUser connectingUser, out IServerUser existingUser)
        {
            lock (_connectedUsers)
            {
                existingUser = _connectedUsers.FirstOrDefault(item => (item.Key.Login == login) && (item.Key != connectingUser)).Key;
                return existingUser != null;
            }
        }


        private IMatchmaking GetMatchmaking(IServerUser user, QueueAction action)
        {
            IMatchmaking matchmaking;
            _matchmaking.TryGetValue(action.GameMode, out matchmaking);
            return matchmaking;
        }

        private void OnClientConnectedJson(object sender, SocketAsyncEventArgs socketAsyncEventArgs)
        {
            lock (_connectedUsers)
            {
                _connectedUsers.TryAdd(new User(socketAsyncEventArgs.AcceptSocket, _logger, this, this, new PacketParserJson<PacketC2S>(_logger, Encoding.Unicode)), DateTime.Now);
            }
        }

        private void OnClientConnectedBson(object sender, SocketAsyncEventArgs socketAsyncEventArgs)
        {
            lock (_connectedUsers)
            {
                _connectedUsers.TryAdd(new User(socketAsyncEventArgs.AcceptSocket, _logger, this, this, new PacketTaskParserBson<PacketC2S>(_logger)), DateTime.Now);
            }
        }

        public void Update()
        {
            foreach (var keyValuePair in _matchmaking)
                keyValuePair.Value.Update();

            foreach (var keyValuePair in _runningGames)
                keyValuePair.Value.Update();
        }
    }
}