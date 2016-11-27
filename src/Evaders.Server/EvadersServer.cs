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
    using Core.Utility;
    using Microsoft.Extensions.Logging;
    using Payloads;

    public class EvadersServer : IServer, IRulesProvider
    {
        public int MaxUsernameLength => _config.MaxUsernameLength;
        public bool ServerListening => _serverSocket.IsBound && !_serverSocket.Stopped;
        private readonly ServerConfiguration _config;
        private readonly ConcurrentDictionary<IServerUser, DateTime> _connectedUsers = new ConcurrentDictionary<IServerUser, DateTime>();
        private readonly ILogger _logger;
        private readonly IMatchmaking _matchmaking;
        private readonly ConcurrentDictionary<long, ServerGame> _runningGames = new ConcurrentDictionary<long, ServerGame>();
        private readonly EasyTaskSocket _serverSocket;
        private readonly IServerSupervisor _supervisor;
        private long _gameIdentifier;
        private long _userIdentifier;


        public EvadersServer(IServerSupervisor supervisor, IMatchmaking matchmaking, ILogger logger, ServerConfiguration config)
        {
            if (!config.IsValid)
                throw new ArgumentException("Invalid config", nameof(config));

            _supervisor = supervisor;
            _matchmaking = matchmaking;
            _logger = logger;
            _matchmaking.OnSuggested += OnMatchmakingFoundMatchup;
            _config = config;


            logger.LogInformation("Setting up tcp accept socket");
            var listener = new TcpListener(IPAddress.Parse(config.IP), config.Port);
            listener.Start();
            _serverSocket = new EasyTaskSocket(listener.Server);
            _serverSocket.OnAccepted += OnClientConnected;
            if (!_serverSocket.StartJobs(EasyTaskSocket.SocketTasks.Accept))
                throw new Exception("Could not start network jobs");
            logger.LogInformation("Server online!");
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

        void IServer.HandleUserEnterQueue(IServerUser user, int count)
        {
            lock (_matchmaking)
            {
                var regCount = _matchmaking.GetRegisterCount(user);
                if (regCount >= _config.MaxQueueCount)
                {
                    user.IllegalAction("Exceeded max queue count: " + _config.MaxQueueCount);
                    return;
                }

                var queueLimitExclusive = Math.Min(_config.MaxQueueCount, regCount + count);
                for (; regCount < queueLimitExclusive; regCount++)
                    _matchmaking.EnterQueue(user);

                user.Send(Packet.PacketTypeS2C.QueueState, regCount);
            }
        }

        void IServer.HandleUserLeaveQueue(IServerUser user, int count)
        {
            lock (_matchmaking)
            {
                for (var i = 0; i < count; i++)
                    _matchmaking.LeaveQueue(user);

                user.Send(Packet.PacketTypeS2C.QueueState, _matchmaking.GetRegisterCount(user));
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
            if (!_runningGames.TryRemove(serverGame.GameIdentifier, out game))
            {
                return;
            }

            if (serverGame.Users.All(usr => !usr.Connected))
                return;

            var winner = serverGame.ValidEntities.Any() ? serverGame.Users.First(usr => usr.Identifier == serverGame.ValidEntities.First().PlayerIdentifier) : null;
            foreach (var serverUser in serverGame.Users)
                serverUser.Send(Packet.PacketTypeS2C.GameEnd, new GameEnd(serverGame.GameIdentifier, serverGame.Users.ToArray(), serverUser.Identifier == winner?.Identifier, winner));
            if (winner == null)
                return;
            foreach (var serverUser in serverGame.Users)
                _supervisor.GameEnded(serverGame, winner.Login, serverGame.Users.Where(usr => usr.Identifier != serverUser.Identifier).Select(usr => usr.Login).ToArray());
        }

        void IServer.HandleGameEndedTurn(ServerGame serverGame)
        {
            _supervisor.GameEndedTurn(serverGame);
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

                var game = new ServerGame(this, matchCreatedArgs.Users, _config.GameSettings, _gameIdentifier, _logger);
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