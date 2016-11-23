namespace Evaders.Server
{
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using CommonNetworking;
    using CommonNetworking.CommonPayloads;
    using Core.Game;
    using Core.Utility;
    using Extensions;
    using Newtonsoft.Json;
    using Payloads;

    internal class ServerGame : DefaultSandboxGame<IServerUser>
    {
        private readonly ILogger _logger;
        private readonly IServer _server;
        private readonly Stopwatch _time = Stopwatch.StartNew();
        private readonly HashSet<IServerUser> _turnEndUsers = new HashSet<IServerUser>();

        [JsonProperty] public readonly long GameIdentifier;

        private double _lastFrameSec;

        public ServerGame(IServer server, IEnumerable<IServerUser> users, GameSettings settings, long gameIdentifier, ILogger logger) : base(users, settings)
        {
            _server = server;
            GameIdentifier = gameIdentifier;
            _logger = logger;
        }

        public void Update()
        {
            var time = _time.Elapsed.TotalSeconds;
            var elapsed = time - _lastFrameSec;

            if (elapsed > Settings.MaxFrameTimeSec)
            {
                _logger.Write($"Forcing advancement of game {GameIdentifier}", Severity.Trace);
                foreach (var user in Users.Where(usr => usr.Connected && !_turnEndUsers.Contains(usr)))
                {
                    _turnEndUsers.Add(user);
                    OnIllegalAction(user, $"You took too long for your turn. The longest you may think is: {Settings.MaxFrameTimeSec} sec. You skipped the turn!");
                    //user.Dispose(); // rip socket
                }
                PossibleEndTurn();
            }
        }

        public void UserRequestsEndTurn(IServerUser @from)
        {
            if (_turnEndUsers.Contains(from))
            {
                OnIllegalAction(from, "Please wait for others to get ready. No need to spam! In fact, it could cost you a turn :) (Stop spamming EndTurn)");
                return;
            }
            if (!HasUser(@from))
            {
                OnIllegalAction(from, "You can't end your turn in a game you don't even play in");
                return;
            }
            _turnEndUsers.Add(@from);

            PossibleEndTurn();
        }

        private void PossibleEndTurn()
        {
            if (Users.Where(usr => usr.Connected).All(usr => _turnEndUsers.Contains(usr)))
            {
                if (Users.All(usr => !usr.Connected))
                {
                    OnGameEnd();
                    return;
                }
                NextTurn();
            }
        }

        public void AddGameAction(IServerUser user, GameAction action)
        {
            AddAction(user, action);
        }

        //public void UserRequestsEndTurn(User from)
        //{
        //    if (_turnEndUsers.Contains(from))
        //    {
        //        from.Send(new PacketS2C(PacketS2C.PacketTypeS2C.IllegalAction, "Please wait for others to get ready. No need to spam! In fact, it could cost you a turn :)"));
        //        return;
        //    }
        //    if (!_users.ContainsKey(@from))
        //    {
        //        from.Send(new PacketS2C(PacketS2C.PacketTypeS2C.IllegalAction, "You can't end your turn in a game you don't even play in"));
        //        return;
        //    }
        //    _turnEndUsers.Add(from);

        //    if (_users.Where(usr => usr.Key.Connected).All(usr => _turnEndUsers.Contains(usr.Key)))
        //        return;


        //    Replay.NextFrame();

        //    foreach (var entity in Entities)
        //        entity.Update();

        //    foreach (var projectile in Projectiles)
        //        projectile.Update();

        //    foreach (var removeEntity in _toRemoveEntities)
        //        Entities.Remove(removeEntity);

        //    foreach (var removeProjectile in _toRemoveProjectiles)
        //        Projectiles.Remove(removeProjectile);

        //    foreach (var keyValuePair in _users)
        //        keyValuePair.Value.Clear();

        //    if (GameEnded)
        //    {
        //        var hasWinner = Entities.Any();
        //        var winner = hasWinner ? _users.First(item => item.Key.Identifier == Entities.First().PlayerIdentifier).Key.Username : "None";
        //        var winnerPublicId = hasWinner ? (long?)Entities.First().PlayerIdentifier : null;
        //        var gameResult = new MatchHistory.FinishedMatch(_users.Select(item => item.Key.Username).ToArray(), _server.GetRunningGameId(this), Turn * TimePerFrameSec, winner, winnerPublicId);
        //        Broadcast(new PacketS2C(PacketS2C.PacketTypeS2C.GameEnd, gameResult));
        //        _server.EndGame(this, gameResult);
        //    }
        //    else
        //    {
        //        foreach (var user in _users.Where(user => user.Key.FullGameState))
        //            user.Key.Send(new PacketS2C(PacketS2C.PacketTypeS2C.GameState, new GameState(Identifier, this)));
        //        Broadcast(new PacketS2C(PacketS2C.PacketTypeS2C.NextRound, Identifier));
        //    }
        //}


        //public void OnGameEnd(Game game)
        //{
        //    var hasWinner = Entities.Any();
        //    var winner = hasWinner ? _users.First(item => item.Key.Identifier == Entities.First().PlayerIdentifier).Key.Username : "None";
        //    var winnerPublicId = hasWinner ? (long?)Entities.First().PlayerIdentifier : null;
        //    var gameResult = new MatchHistory.FinishedMatch(_users.Select(item => item.Key.Username).ToArray(), _server.GetRunningGameId(this), Turn * TimePerFrameSec, winner, winnerPublicId);
        //    Broadcast(new PacketS2C(PacketS2C.PacketTypeS2C.GameEnd, gameResult));
        //    _server.EndGame(this, gameResult);
        //}

        public void HandleReconnect(IServerUser user)
        {
            if (HasUser(user))
                user.Send(Packet.PacketTypeS2C.GameState, new GameState(GameIdentifier, this, user.Identifier));
        }

        protected override void OnActionExecuted(IServerUser @from, GameAction action)
        {
            foreach (var serverUser in Users.Where(user => !user.FullGameState))
                serverUser.Send(Packet.PacketTypeS2C.GameAction, action.AsLiveAction(GameIdentifier));
        }

        protected override void OnTurnEnded()
        {
            _turnEndUsers.Clear();
            foreach (var user in Users.Where(user => user.FullGameState))
                user.Send(Packet.PacketTypeS2C.GameState, new GameState(GameIdentifier, this, user.Identifier));
            foreach (var serverUser in Users)
                serverUser.Send(Packet.PacketTypeS2C.NextRound, GameIdentifier);
            _lastFrameSec = _time.Elapsed.TotalSeconds;
            _server.HandleGameEndedTurn(this);
        }

        protected override void OnGameEnd()
        {
            _server.HandleGameEnded(this);
        }

        protected override void OnIllegalAction(IServerUser user, string warningMsg)
        {
            user.Send(Packet.PacketTypeS2C.IllegalAction, new IllegalAction(warningMsg, true, GameIdentifier));
        }

        protected override bool BeforeHandleAction(IServerUser @from, GameAction action)
        {
            if (_turnEndUsers.Contains(from))
            {
                OnIllegalAction(from, "Please wait for others to get ready. No need to spam! In fact, it could cost you a turn :)");
                return false;
            }
            if (!HasUser(@from))
            {
                OnIllegalAction(from, "You can't end your turn in a game you don't even play in");
                return false;
            }

            return true;
        }
    }
}