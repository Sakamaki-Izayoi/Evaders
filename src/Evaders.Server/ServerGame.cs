namespace Evaders.Server
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using CommonNetworking;
    using CommonNetworking.CommonPayloads;
    using Core.Game;
    using Extensions;
    using Integration;
    using JetBrains.Annotations;
    using Microsoft.Extensions.Logging;
    using Newtonsoft.Json;
    using Payloads;

    public class ServerGame : DefaultSandboxGame<IServerUser>
    {
        private readonly ILogger _logger;
        private readonly IServer _server;
        private readonly IServerSupervisor _supervisor;
        private readonly Stopwatch _time = Stopwatch.StartNew();
        private readonly ConcurrentDictionary<IServerUser, bool> _turnEndUsers = new ConcurrentDictionary<IServerUser, bool>();

        private readonly object _updateLock = new object();

        [JsonProperty] public readonly long GameIdentifier;

        private double _lastFrameSec;

        public ServerGame([NotNull] IServer server, [NotNull] IServerSupervisor supervisor, [NotNull] IEnumerable<IServerUser> users, [NotNull] GameSettings settings, long gameIdentifier, [NotNull] ILogger logger) : base(users, settings)
        {
            if (server == null)
                throw new ArgumentNullException(nameof(server));
            if (supervisor == null)
                throw new ArgumentNullException(nameof(supervisor));
            if (users == null)
                throw new ArgumentNullException(nameof(users));
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));
            if (logger == null)
                throw new ArgumentNullException(nameof(logger));

            _server = server;
            _supervisor = supervisor;
            GameIdentifier = gameIdentifier;
            _logger = logger;

            InitTurnEndStates();
        }

        public void Update()
        {
            lock (_updateLock)
            {
                var time = _time.Elapsed.TotalSeconds;
                var elapsed = time - _lastFrameSec;

                if (elapsed > Settings.MaxTurnTimeSec)
                    lock (NextTurnLock)
                    {
                        if (elapsed > Settings.MaxTurnTimeSec)
                        {
                            _logger.LogTrace($"Forcing advancement of game {GameIdentifier}");
                            foreach (var user in Users.Where(usr => usr.Connected && !IsUserReady(usr)))
                                //_turnEndUsers[user] = true;
                                OnIllegalAction(user, $"You took too long for your turn. The longest you may think is: {Settings.MaxTurnTimeSec} sec. You skipped the turn!");
                            //user.Dispose(); // rip socket
                            NextTurn();
                        }
                    }
            }
        }

        public void UserRequestsEndTurn(IServerUser from)
        {
            lock (NextTurnLock) // must not process the request during a nextturn
            {
                if (IsUserReady(from))
                {
                    OnIllegalAction(from, "Please wait for others to get ready. No need to spam! In fact, it could cost you a turn :) (Stop spamming EndTurn)");
                    return;
                }
                if (!HasUser(from))
                {
                    OnIllegalAction(from, "You can't end your turn in a game you don't even play in");
                    return;
                }
                _turnEndUsers[from] = true;
            }


            PossibleEndTurn(); // This is not inside the lock on purpose (deadlock)
        }

        private bool IsUserReady(IServerUser user)
        {
            bool ready;
            if (!_turnEndUsers.TryGetValue(user, out ready))
            {
                _logger.LogWarning($"Either {user} is being a bad boy or the {nameof(_turnEndUsers)} dict is bad.");
            }
            return ready;
        }

        private void PossibleEndTurn()
        {
            lock (NextTurnLock) // Need to apply the next turn lock or it will meet the next turn requirement while nextturn is executed, causing it to call nextturn too often
            {
                if (Users.Where(usr => usr.Connected).All(IsUserReady))
                {
                    if (Users.All(usr => !usr.Connected))
                    {
                        OnGameEnd();
                        return;
                    }
                    NextTurn();
                }
            }
        }

        public void AddGameAction(IServerUser user, GameAction action)
        {
            if (action == null)
            {
                OnIllegalAction(user, "Invalid game action");
                return;
            }

            AddAction(user, action);
        }


        public void HandleReconnect(IServerUser user)
        {
            if (HasUser(user))
                lock (NextTurnLock)
                {
                    user.Send(Packet.PacketTypeS2C.GameState, new GameState(GameIdentifier, this, user.Identifier));
                }
        }

        protected override void OnActionExecuted(IServerUser from, GameAction action)
        {
            foreach (var serverUser in Users.Where(user => !user.FullGameState))
                serverUser.Send(Packet.PacketTypeS2C.ConfirmedGameAction, action);
        }

        private void InitTurnEndStates()
        {
            foreach (var usr in Users)
                _turnEndUsers[usr] = false;
        }

        protected override void OnTurnEnded()
        {
            InitTurnEndStates();
            foreach (var user in Users.Where(user => user.FullGameState))
                user.Send(Packet.PacketTypeS2C.GameState, new GameState(GameIdentifier, this, user.Identifier));
            foreach (var serverUser in Users)
                serverUser.Send(Packet.PacketTypeS2C.NextTurn);
            _lastFrameSec = _time.Elapsed.TotalSeconds;
            _supervisor.GameEndedTurn(this);
        }

        protected override void OnGameEnd()
        {
            _server.HandleGameEnded(this);

            if (Users.All(usr => !usr.Connected))
                return;

            var winner = Entities.Any() ? Users.First(usr => usr.Identifier == Entities.First().PlayerIdentifier) : null;
            foreach (var serverUser in Users)
                serverUser.Send(Packet.PacketTypeS2C.GameEnd, new GameEnd(GameIdentifier, Users.ToArray(), serverUser.Identifier == winner?.Identifier, winner));
            if (winner == null)
                return;

            _supervisor.GameEnded(this, winner.Login, Users.Where(usr => usr.Identifier != winner.Identifier).Select(usr => usr.Login).ToArray());

            foreach (var serverUser in Users)
            {
                serverUser.SetIngame(null);
            }
        }

        protected override void OnIllegalAction(IServerUser user, string warningMsg)
        {
            user.Send(Packet.PacketTypeS2C.IllegalAction, new IllegalAction(warningMsg, true, GameIdentifier));
        }

        protected override bool BeforeHandleAction(IServerUser from, GameAction action)
        {
            if (!HasUser(from))
            {
                OnIllegalAction(from, "You can't end your turn in a game you don't even play in");
                _logger.LogWarning($"Potential cheat attempt by {from}: tried to take action in other game");
                return false;
            }
            if (IsUserReady(from))
            {
                OnIllegalAction(from, "Please wait for others to get ready. No need to spam! In fact, it could cost you a turn :)");
                return false;
            }

            return true;
        }
    }
}