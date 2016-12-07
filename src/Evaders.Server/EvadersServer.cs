namespace Evaders.Server
{
    using System;
    using System.Collections.Concurrent;
    using System.Linq;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading;
    using CommonNetworking;
    using CommonNetworking.CommonPayloads;
    using Core.Game;
    using Integration;
    using JetBrains.Annotations;
    using Microsoft.Extensions.Logging;

    public class EvadersServer : IServer, IRulesProvider
    {
        public string Motd => _config.Motd;
        public int MaxQueueCount => _config.MaxQueueCount;
        public string[] GameModes => _config.GameModes;
        public int MaxUsernameLength => _config.MaxUsernameLength;
        public bool ServerListening => _serverSocket.IsBound && !_serverSocket.Stopped;
        public bool BsonServerListening => _serverSocketBson.IsBound && !_serverSocketBson.Stopped;
        private readonly ServerSettings _config;
        private readonly ConcurrentDictionary<IServerUser, DateTime> _connectedUsers = new ConcurrentDictionary<IServerUser, DateTime>();
        private readonly ILogger _logger;
        private readonly ConcurrentDictionary<string, IMatchmaking> _matchmaking;
        private readonly ConcurrentDictionary<long, ServerGame> _runningGames = new ConcurrentDictionary<long, ServerGame>();
        private readonly IProviderFactory<GameSettings> _settingsFactory;
        private readonly IProviderFactory<IServerSupervisor> _supervisorFactory;
        private long _gameIdentifier;
        private EasyTaskSocket _serverSocket;
        private EasyTaskSocket _serverSocketBson;
        private long _userIdentifier;


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
            _logger = logger;

            _matchmaking = new ConcurrentDictionary<string, IMatchmaking>(config.GameModes.ToDictionary(item => item, matchmakingFactory.Create));
            foreach (var keyValuePair in _matchmaking)
            {
                keyValuePair.Value.Supervisor = _supervisorFactory.Create(keyValuePair.Key);
                keyValuePair.Value.OnSuggested += OnMatchmakingFoundMatchup;
            }
            _config = config;
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

        public void Start()
        {
            if ((_serverSocket != null) || (_serverSocketBson != null))
                throw new InvalidOperationException("You can only start the server once");

            _logger.LogInformation("Setting up tcp accept socket");

            var listener = new TcpListener(_config.IP, _config.Port);
            var listenerBson = new TcpListener(_config.IP, _config.BSONPort);

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
                _connectedUsers.TryAdd(new User(socketAsyncEventArgs.AcceptSocket, _logger, this, this, new PacketTaskParser<PacketC2S>(_logger, Encoding.Unicode)), DateTime.Now);
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
            foreach (var keyValuePair in _runningGames)
                keyValuePair.Value.Update();
        }

        private void OnMatchmakingFoundMatchup(object sender, Matchmaking.MatchCreatedArgs matchCreatedArgs)
        {
            lock (_runningGames)
            {
                _gameIdentifier++;

                var game = new ServerGame(this, _supervisorFactory.Create(matchCreatedArgs.GameMode), matchCreatedArgs.Users, _settingsFactory.Create(matchCreatedArgs.GameMode), _gameIdentifier, _logger);
                foreach (var serverUser in matchCreatedArgs.Users)
                {
                    matchCreatedArgs.Source.LeaveQueue(serverUser);
                    serverUser.SetIngame(game);
                    game.HandleReconnect(serverUser);
                }
                if (!_runningGames.TryAdd(_gameIdentifier, game))
                    _logger.LogError($"Cannot add game with ID: {game.GameIdentifier}, current is: {_gameIdentifier}");
            }
        }
    }
}