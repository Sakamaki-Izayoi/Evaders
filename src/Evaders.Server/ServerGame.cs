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
    using Core.Utility;
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
        private readonly GameChangeTracker _tracker;

        [JsonProperty]
        public readonly long GameIdentifier;

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

            if (_supervisor.IsRecording) _tracker = new GameChangeTracker();

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
                                OnIllegalAction(user, $"You took too long for your turn. The longest you may think is: {Settings.MaxTurnTimeSec} sec. You skipped the turn!");

                            NextTurn();
                        }
                    }
            }
        }

        public void UserRequestsEndTurn(IServerUser from, int turn = -1)
        {
            if (turn >= 0 && turn != Turn)
            {
                _logger.LogDebug($"Rejecting actions of lagging user: {from} (response after turn end)");
                return;
            }

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

                PossibleEndTurn();
            }
        }

        private bool IsUserReady(IServerUser user)
        {
            bool ready;
            if (!_turnEndUsers.TryGetValue(user, out ready))
                _logger.LogWarning($"Either {user} is being a bad boy or the {nameof(_turnEndUsers)} dict is bad.");
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

        public void AddGameAction(IServerUser user, LiveGameAction action)
        {
            if (action == null)
            {
                OnIllegalAction(user, "Invalid game action");
                return;
            }
            else if (action.Turn >= 0 && action.Turn != Turn)
            {
                _logger.LogDebug($"Dropping action of lagging user: {user}, {action}");
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
            _tracker?.ExecutedGameActions.Add(action);
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
                serverUser.Send(Packet.PacketTypeS2C.NextTurn, new TurnEnd(Turn));
            _lastFrameSec = _time.Elapsed.TotalSeconds;

            _supervisor.GameEndedTurn(this, _tracker);

            _tracker?.ChangedEntities.Clear();// = new List<Tuple<EntityBase, ChangeKind>>();
            _tracker?.ChangedProjectiles.Clear();// = new List<Tuple<Projectile, ChangeKind>>();
            _tracker?.ExecutedGameActions.Clear();// = new List<GameAction>();
            _tracker?.ChangedOrbSpawners.Clear();// = new List<OrbSpawn>();
        }

        protected override Entity SpawnEntity(Vector2 position, long playerIdentifier, CharacterData charData)
        {
            var entity = base.SpawnEntity(position, playerIdentifier, charData);
            _tracker?.ChangedEntities.Add(new Tuple<EntityBase, GameChangeTracker.ChangeKind>(entity, GameChangeTracker.ChangeKind.Spawn));
            return entity;
        }

        protected override Projectile SpawnProjectile(Vector2 direction, EntityBase entity)
        {
            var projectile = base.SpawnProjectile(direction, entity);
            _tracker?.ChangedProjectiles.Add(new Tuple<Projectile, GameChangeTracker.ChangeKind>(projectile, GameChangeTracker.ChangeKind.Spawn));
            return projectile;
        }

        protected override void HandleDeath(EntityBase entity)
        {
            _tracker?.ChangedEntities.Add(new Tuple<EntityBase, GameChangeTracker.ChangeKind>(entity, GameChangeTracker.ChangeKind.Death));
            base.HandleDeath(entity);
        }

        protected override void HandleDeath(Projectile projectile)
        {
            _tracker?.ChangedProjectiles.Add(new Tuple<Projectile, GameChangeTracker.ChangeKind>(projectile, GameChangeTracker.ChangeKind.Death));
            base.HandleDeath(projectile);
        }

        protected override void HandleEntityHealthChanged(EntityBase entity)
        {
            _tracker?.ChangedEntities.Add(new Tuple<EntityBase, GameChangeTracker.ChangeKind>(entity, GameChangeTracker.ChangeKind.Health));
            base.HandleEntityHealthChanged(entity);
        }

        protected override void HandleOrbChangedState(OrbSpawn spawn)
        {
            _tracker?.ChangedOrbSpawners.Add(spawn);
            base.HandleOrbChangedState(spawn);
        }

        protected override void OnGameEnd()
        {
            _server.HandleGameEnded(this);

            if (Users.All(usr => !usr.Connected))
                return;

            var winner = Entities.Any() ? Users.First(usr => usr.Identifier == Entities.First().PlayerIdentifier) : null;
            foreach (var serverUser in Users)
                serverUser.Send(Packet.PacketTypeS2C.GameEnd, new GameEnd(GameIdentifier, Users.ToArray(), serverUser.Identifier == winner?.Identifier, winner?.Identifier ?? -1));
            if (winner == null)
                return;

            _supervisor.GameEnded(this, winner.Login, Users.Where(usr => usr.Identifier != winner.Identifier).Select(usr => usr.Login).ToArray());

            foreach (var serverUser in Users)
                serverUser.SetIngame(null);
        }

        protected override void OnIllegalAction(IServerUser user, string warningMsg)
        {
            user.Send(Packet.PacketTypeS2C.IllegalAction, new IllegalAction(warningMsg, true));
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