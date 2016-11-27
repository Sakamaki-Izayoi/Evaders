namespace Evaders.Server
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading;
    using CommonNetworking;
    using CommonNetworking.CommonPayloads;
    using Core.Game;
    using Integration;
    using JetBrains.Annotations;
    using Microsoft.Extensions.Logging;
    using Payloads;

    public class EvadersServer : IServer, IRulesProvider
    {
        public int MaxUsernameLength => _config.MaxUsernameLength;
        public bool ServerListening => _serverSocket.IsBound && !_serverSocket.Stopped;
        private readonly ServerSettings _config;
        private readonly ConcurrentDictionary<IServerUser, DateTime> _connectedUsers = new ConcurrentDictionary<IServerUser, DateTime>();
        private readonly ILogger _logger;
        private readonly ConcurrentDictionary<long, ServerGame> _runningGames = new ConcurrentDictionary<long, ServerGame>();
        private EasyTaskSocket _serverSocket;
        private readonly IProviderFactory<IServerSupervisor> _supervisorFactory;
        private readonly IProviderFactory<GameSettings> _settingsFactory;
        private long _gameIdentifier;
        private long _userIdentifier;
        private ConcurrentDictionary<string, IMatchmaking> _matchmaking;


        public EvadersServer([NotNull] IProviderFactory<IServerSupervisor> supervisorFactoryFactory, [NotNull] IProviderFactory<GameSettings> settingsFactory, [NotNull] IProviderFactory<IMatchmaking> matchmakingFactory, [NotNull] ILogger logger, [NotNull] ServerSettings config)
        {
            if (supervisorFactoryFactory == null)
                throw new ArgumentNullException(nameof(supervisorFactoryFactory));
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

            _supervisorFactory = supervisorFactoryFactory;
            _settingsFactory = settingsFactory;
            _logger = logger;

            _matchmaking = new ConcurrentDictionary<string, IMatchmaking>(config.GameModes.ToDictionary(item => item, matchmakingFactory.Create));
            foreach (var keyValuePair in _matchmaking)
            {
                keyValuePair.Value.OnSuggested += OnMatchmakingFoundMatchup;
            }
            _config = config;
        }

        public void Start()
        {
            if (_serverSocket != null)
                throw new InvalidOperationException("You can only start the server once");

            _logger.LogInformation("Setting up tcp accept socket");
            var listener = new TcpListener(_config.IP, _config.Port);
            listener.Start();
            _serverSocket = new EasyTaskSocket(listener.Server);
            _serverSocket.OnAccepted += OnClientConnected;

            if (!_serverSocket.StartJobs(EasyTaskSocket.SocketTasks.Accept))
                throw new Exception("Could not start network jobs");

            _logger.LogInformation("Server online!");
        }

        void IServer.HandleUserAction(IServerUser @from, LiveGameAction action)
        {
            ServerGame game;
            if (!_runningGames.TryGetValue(action.GameIdentifier, out game))
            {
                @from.IllegalAction("You cannot take action in a game you don't even play in!");
                return;
            }

            game.AddGameAction(from, action);
        }


        public bool WouldAuthCollide(Guid login, IServerUser connectingUser, out IServerUser existingUser)
        {
            lock (_connectedUsers)
            {
                existingUser = _connectedUsers.FirstOrDefault(item => item.Key.Login == login && item.Key != connectingUser).Key;
                return existingUser != null;
            }
        }

        public long GenerateUniqueUserIdentifier()
        {
            return Interlocked.Increment(ref _userIdentifier);
        }

        void IServer.HandleUserEndTurn(IServerUser user, long gameIdentifier)
        {
            ServerGame game;
            if (!_runningGames.TryGetValue(gameIdentifier, out game))
            {
                user.IllegalAction("You can't end your turn in a game you don't even play in: " + gameIdentifier);
                return;
            }
            game.UserRequestsEndTurn(user);
        }

        void IServer.HandleUserEnterQueue(IServerUser user, QueueAction action)
        {
            var matchmaking = GetMatchmaking(user, action);
            if (matchmaking == null)
                return;

            lock (matchmaking)
            {
                var regCount = matchmaking.GetRegisterCount(user);
                if (regCount >= _config.MaxQueueCount)
                {
                    user.IllegalAction("Exceeded max queue action: " + _config.MaxQueueCount);
                    return;
                }

                var queueLimitExclusive = Math.Min(_config.MaxQueueCount, regCount + action.Count);
                for (; regCount < queueLimitExclusive; regCount++)
                    matchmaking.EnterQueue(user);

                user.Send(Packet.PacketTypeS2C.QueueState, regCount);
            }
        }

        private IMatchmaking GetMatchmaking(IServerUser user, QueueAction action)
        {
            if (action.Count <= 0)
            {
                user.IllegalAction("Invalid queue count");
                return null;
            }

            IMatchmaking matchmaking;
            if (!_matchmaking.TryGetValue(action.GameMode, out matchmaking))
            {
                user.IllegalAction("There is no such gamemode");
                return null;
            }
            return matchmaking;
        }

        void IServer.HandleUserLeaveQueue(IServerUser user, QueueAction action)
        {
            var matchmaking = GetMatchmaking(user, action);
            if (matchmaking == null)
                return;

            lock (matchmaking)
            {
                var regCount = matchmaking.GetRegisterCount(user);
                if (action.Count >= regCount)
                    matchmaking.LeaveQueueCompletely(user);
                else
                    for (var i = 0; i < action.Count; i++)
                        matchmaking.LeaveQueue(user);

                user.Send(Packet.PacketTypeS2C.QueueState, Math.Max(0, regCount - action.Count));
            }
        }

        void IServer.HandleUserReconnect(IServerUser user)
        {
            foreach (var game in _runningGames.Where(game => game.Value.HasUser(user)))
                game.Value.HandleReconnect(user);
        }

        void IServer.HandleUserResync(IServerUser user, long gameIdentifier)
        {
            ServerGame game;
            if (!_runningGames.TryGetValue(gameIdentifier, out game))
            {
                _logger.LogWarning($"User tried resyncing in an unknown game: {gameIdentifier}");
                user.IllegalAction($"Cannot resync in game: {gameIdentifier}, because it does not exist!");
                return;
            }
            game.HandleReconnect(user);
        }

        void IServer.Kick(IServerUser user)
        {
            DateTime connectedTime;
            lock (_connectedUsers)
            {
                _connectedUsers.TryRemove(user, out connectedTime);
            }
            if (user.Connected)
                user.Dispose();

            _logger.LogDebug($"Kicking user: {user}, who connected {connectedTime}");

            // Todo: instead of letting running games wait for the timeout, kick the user and his entities out right away
        }

        string IServer.GetMotd()
        {
            return _config.Motd;
        }

        void IServer.HandleGameEnded(ServerGame serverGame)
        {
            ServerGame game;
            _runningGames.TryRemove(serverGame.GameIdentifier, out game);
        }

        public string[] GetGameModes()
        {
            return _config.GameModes;
        }

        private void OnClientConnected(object sender, SocketAsyncEventArgs socketAsyncEventArgs)
        {
            lock (_connectedUsers)
            {
                _connectedUsers.TryAdd(new User(socketAsyncEventArgs.AcceptSocket, _logger, this, this), DateTime.Now);
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
                    game.HandleReconnect(serverUser);
                }
                _runningGames.TryAdd(_gameIdentifier, game);
            }
        }
    }
}