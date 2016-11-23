namespace Evaders.Server
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Sockets;
    using CommonNetworking;
    using CommonNetworking.CommonPayloads;
    using Core.Utility;
    using Payloads;

    public class EvadersServer : IServer, IRulesProvider
    {
        public int MaxUsernameLength => _config.MaxUsernameLength;
        public IEnumerable<IServerUser> ConnectedUsers => _connectedUsers;
        private readonly ServerConfiguration _config;
        private readonly List<IServerUser> _connectedUsers = new List<IServerUser>();
        private readonly List<long> _endedGamesThisFrame = new List<long>();
        private readonly ILogger _logger;
        private readonly IMatchmaking _matchmaking;
        private readonly Dictionary<long, ServerGame> _runningGames = new Dictionary<long, ServerGame>();
        private readonly EasySocket _serverSocket;
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


            logger.Write("Setting up tcp accept socket");
            var listener = new TcpListener(IPAddress.Parse(config.IP), config.Port);
            listener.Start();
            _serverSocket = new EasySocket(listener.Server);
            _serverSocket.OnAccepted += OnClientConnected;
            if (!_serverSocket.StartJobs(EasySocket.SocketTasks.Accept))
                throw new Exception("Could not start network jobs");
            logger.Write("Server online!");
        }

        void IServer.HandleUserAction(IServerUser @from, LiveGameAction action)
        {
            if (!_runningGames.ContainsKey(action.GameIdentifier))
            {
                @from.IllegalAction("You cannot take action in a game you don't even play in!");
                return;
            }

            _runningGames[action.GameIdentifier].AddGameAction(from, action);
        }


        public long GenerateUniqueUserIdentifier()
        {
            return _userIdentifier++;
        }

        void IServer.HandleUserEndTurn(IServerUser user, long gameIdentifier)
        {
            if (!_runningGames.ContainsKey(gameIdentifier))
            {
                user.IllegalAction("You can't end your turn in a game you don't even play in: " + gameIdentifier);
                return;
            }
            _runningGames[gameIdentifier].UserRequestsEndTurn(user);
        }

        void IServer.HandleUserEnterQueue(IServerUser user, int count)
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

        void IServer.HandleUserLeaveQueue(IServerUser user, int count)
        {
            for (var i = 0; i < count; i++)
                _matchmaking.LeaveQueue(user);

            user.Send(Packet.PacketTypeS2C.QueueState, _matchmaking.GetRegisterCount(user));
        }

        void IServer.HandleUserReconnect(IServerUser user)
        {
            foreach (var game in _runningGames.Where(game => game.Value.HasUser(user)))
                game.Value.HandleReconnect(user);
        }

        void IServer.HandleUserResync(IServerUser user, long gameIdentifier)
        {
            if (!_runningGames.ContainsKey(gameIdentifier))
            {
                _logger.Write("User tried resyncing in an unknown game: " + gameIdentifier, Severity.Warning);
                user.IllegalAction("Cannot resync in game: " + gameIdentifier + ", because it does not exist!");
                return;
            }
            _runningGames[gameIdentifier].HandleReconnect(user);
        }

        void IServer.Kick(IServerUser user)
        {
            _connectedUsers.Remove(user);
            if (user.Connected)
                user.Dispose();
            // Todo: instead of letting running games wait for the timeout, kick the user and his entities out right away
        }

        string IServer.GetMotd()
        {
            return _supervisor.GetMotd();
        }

        void IServer.HandleGameEnded(ServerGame serverGame)
        {
            _endedGamesThisFrame.Add(serverGame.GameIdentifier);
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
            _connectedUsers.Add(new User(socketAsyncEventArgs.AcceptSocket, _logger, this, this));
        }

        public void Update()
        {
            _serverSocket.Work();
            for (var index = 0; index < _connectedUsers.Count; index++)
            {
                var connectedUser = _connectedUsers[index];
                try
                {
                    connectedUser.Update();
                }
                catch (Exception exception)
                {
                    if (!connectedUser.Disposed)
                    {
                        _logger.Write($"Kicking user due to exception: {exception}", Severity.Warning);
                        connectedUser.Dispose();
                    }
                    else
                        _logger.Write($"Removing disconnected user: {connectedUser}", Severity.Debug);

                    _connectedUsers.Remove(connectedUser);
                    index--;
                }
            }
            foreach (var keyValuePair in _runningGames)
                keyValuePair.Value.Update();
            while (_endedGamesThisFrame.Count > 0)
            {
                _runningGames.Remove(_endedGamesThisFrame[0]);
                _endedGamesThisFrame.RemoveAt(0);
            }
        }

        private void OnMatchmakingFoundMatchup(object sender, Matchmaking.MatchCreatedArgs matchCreatedArgs)
        {
            _gameIdentifier++;

            var game = new ServerGame(this, matchCreatedArgs.Users, _config.GameSettings, _gameIdentifier, _logger);
            foreach (var serverUser in matchCreatedArgs.Users)
            {
                matchCreatedArgs.Source.LeaveQueue(serverUser);
                game.HandleReconnect(serverUser);
            }
            _runningGames.Add(_gameIdentifier, game);
        }
    }
}