namespace Evaders.Core.Game
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.Serialization;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Serialization;
    using Utility;

    public abstract class Game<TUser> : GameBase where TUser : IUser
    {
        public override double TimePerFrameSec => 1d / Settings.TurnsPerSecond;
        public bool GameEnded => _entities.All(entity => entity.Value.PlayerIdentifier == _entities.FirstOrDefault().Value?.PlayerIdentifier);

        [JsonProperty]
        public IEnumerable<TUser> Users => _users.Keys;

        [JsonProperty]
        public override IEnumerable<EntityBase> Entities => _entities.Values;

        [JsonProperty]
        public override IEnumerable<Projectile> Projectiles => _projectiles.Values;

        [JsonProperty]
        public override IEnumerable<HealOrbSpawn> HealSpawns => _healOrbs;

        [JsonProperty]
        public override CloneOrbSpawn ClonerSpawn => _clonerSpawn;

        protected IEnumerable<Entity> EntitiesInternal => _entities.Values;

        private readonly ConcurrentDictionary<long, Entity> _entities = new ConcurrentDictionary<long, Entity>();
        private readonly ConcurrentDictionary<long, Projectile> _projectiles = new ConcurrentDictionary<long, Projectile>();
        private readonly HealOrbSpawn[] _healOrbs;
        private readonly CloneOrbSpawn _clonerSpawn;

        private readonly List<Entity> _toRemoveEntities = new List<Entity>();
        private readonly List<Projectile> _toRemoveProjectiles = new List<Projectile>();
        private readonly ConcurrentDictionary<TUser, ConcurrentBag<GameAction>> _users;
        protected readonly object NextTurnLock = new object();

        [JsonProperty("LastEntityIdentifier")]
        private long _entityIdentifier;
        [JsonProperty("LastProjectileIdentifier")]
        private long _projectileIdentifier;

        protected Game(IEnumerable<TUser> users, GameSettings settings, IEnumerable<Entity> entities, IEnumerable<Projectile> projectiles, IEnumerable<HealOrbSpawn> healSpawns, CloneOrbSpawn clonerSpawn, long lastEntityIdentifier, long lastProjectileIdentifier) : base(settings)
        {
            _users = new ConcurrentDictionary<TUser, ConcurrentBag<GameAction>>(users.Select(item => new KeyValuePair<TUser, ConcurrentBag<GameAction>>(item, new ConcurrentBag<GameAction>())));
            _entities = new ConcurrentDictionary<long, Entity>(entities.Select(item => new KeyValuePair<long, Entity>(item.EntityIdentifier, item)));
            _projectiles = new ConcurrentDictionary<long, Projectile>(projectiles.Select(item => new KeyValuePair<long, Projectile>(item.ProjectileIdentifier, item)));
            _healOrbs = healSpawns.ToArray();
            _clonerSpawn = clonerSpawn;
            _entityIdentifier = lastEntityIdentifier;
            _projectileIdentifier = lastProjectileIdentifier;
        }

        protected Game(IEnumerable<TUser> users, GameSettings settings, IMapGenerator generator) : base(settings)
        {
            _users = new ConcurrentDictionary<TUser, ConcurrentBag<GameAction>>(users.Select(item => new KeyValuePair<TUser, ConcurrentBag<GameAction>>(item, new ConcurrentBag<GameAction>())));
            using (var enumerator = generator.GetEntityPositions(_users.Count, settings).GetEnumerator())
            {
                foreach (var user in _users)
                {
                    if (!enumerator.MoveNext()) throw new Exception("Map generator returns less positions than requested");

                    var entityIdentifier = ++_entityIdentifier;
                    _entities.TryAdd(entityIdentifier, new Entity(Settings.DefaultCharacterData, enumerator.Current, user.Key.Identifier, entityIdentifier, this));
                }
            }
            _healOrbs = generator.GetHealorbPositions(_entities.Count, settings).Select(item => new HealOrbSpawn(this, item)).ToArray();
            _clonerSpawn = new CloneOrbSpawn(this, Vector2.Zero);
        }

        protected void NextTurn()
        {
            lock (NextTurnLock)
            {
                foreach (var user in _users)
                    while (!user.Value.IsEmpty)
                    {
                        GameAction gameAction;
                        if (!user.Value.TryTake(out gameAction))
                            continue;

                        Entity controlledEntity;
                        if (!_entities.TryGetValue(gameAction.ControlledEntityIdentifier, out controlledEntity))
                        {
                            OnIllegalAction(user.Key, "You are controlling a not existing entity: " + gameAction.ControlledEntityIdentifier);
                            continue;
                        }
                        if (controlledEntity.PlayerIdentifier != user.Key.Identifier)
                        {
                            OnIllegalAction(user.Key, "You cannot control an enemy untit :)");
                            continue;
                        }

                        bool result;
                        switch (gameAction.Type)
                        {
                            case GameActionType.Move:
                                result = controlledEntity.MoveToInternal(gameAction.Position);
                                break;
                            case GameActionType.Shoot:
                                result = controlledEntity.ShootInternal(gameAction.Position);
                                break;
                            default:
                                OnIllegalAction(user.Key, "Unknown Action: " + (int)gameAction.Type);
                                continue;
                        }
                        if (!result)
                            OnIllegalAction(user.Key, "Illegal action: " + gameAction);
                        else
                            OnActionExecuted(user.Key, gameAction);
                    }

                foreach (var entity in _entities)
                    entity.Value.UpdateMovement();

                foreach (var projectile in _projectiles)
                    projectile.Value.UpdateMovement();

                foreach (var healOrbSpawn in _healOrbs)
                    healOrbSpawn.Update();

                _clonerSpawn.Update();

                foreach (var entity in _entities)
                    entity.Value.UpdateCombat();

                foreach (var projectile in _projectiles)
                    projectile.Value.UpdateCombat();

                foreach (var removeEntity in _toRemoveEntities)
                {
                    Entity removed;
                    _entities.TryRemove(removeEntity.EntityIdentifier, out removed);
                }
                _toRemoveEntities.Clear();

                foreach (var removeProjectile in _toRemoveProjectiles)
                {
                    Projectile removed;
                    _projectiles.TryRemove(removeProjectile.ProjectileIdentifier, out removed);
                }
                _toRemoveProjectiles.Clear();

                Turn++;

                if (GameEnded)
                    OnGameEnd();
                else
                    OnTurnEnded();
            }
        }

        /// <summary>
        ///     Just adds the action, but does not call any checks like BeforeHandleAction
        /// </summary>
        /// <param name="from"></param>
        /// <param name="action"></param>
        protected void AddActionInternal(TUser from, GameAction action)
        {
            lock (NextTurnLock)
            {
                _users[from].Add(action);
            }
        }

        /// <summary>
        ///     Checks with BeforeHandleAction if the action is legit, then passes the call to AddActionInternal and returns
        ///     accordingly
        /// </summary>
        /// <param name="from"></param>
        /// <param name="action"></param>
        /// <returns></returns>
        protected internal virtual bool AddAction(TUser from, GameAction action)
        {
            if (!BeforeHandleAction(from, action))
                return false;

            AddActionInternal(from, action);
            return true;
        }

        protected internal override bool AddAction(long userIdentifier, GameAction action)
        {
            var user = GetUser(userIdentifier);
            if (user == null)
                throw new ArgumentException("User does not exist: " + userIdentifier, nameof(userIdentifier));
            return AddAction(user, action);
        }

        protected internal override void SpawnProjectile(Vector2 direction, EntityBase entity)
        {
            var projectileIdentifier = ++_projectileIdentifier;
            if (!_projectiles.TryAdd(projectileIdentifier, new Projectile(direction.Unit, entity, this, projectileIdentifier)))
                throw new Exception("Could not spawn projectile with id: " + projectileIdentifier);
        }

        protected internal override Entity SpawnEntity(Vector2 position, long playerIdentifier, CharacterData charData)
        {
            var entityIdentifier = ++_entityIdentifier;
            var entity = new Entity(charData, position, playerIdentifier, entityIdentifier, this);
            if (!_entities.TryAdd(entityIdentifier, entity))
                throw new Exception("Could not spawn entity with id: " + entityIdentifier);
            return entity;
        }

        public EntityBase GetEntityById(long entityIdentifier)
        {
            Entity entity;
            _entities.TryGetValue(entityIdentifier, out entity);
            return entity;
        }

        public Projectile GetProjectileById(long projectileIdentifier)
        {
            Projectile projectile;
            _projectiles.TryGetValue(projectileIdentifier, out projectile);
            return projectile;
        }

        internal TUser GetUser(long userIdentifier) => _users.Keys.FirstOrDefault(item => item.Identifier == userIdentifier);

        protected internal override void HandleDeath(EntityBase entity)
        {
            _toRemoveEntities.Add((Entity)entity);
        }

        protected internal override void HandleDeath(Projectile projectile)
        {
            _toRemoveProjectiles.Add(projectile);
        }

        [OnDeserialized]
        private void ValidateReferences(StreamingContext context)
        {
            lock (NextTurnLock)
            {
                foreach (var entity in _entities)
                    entity.Value.Game = this;
                foreach (var projectile in _projectiles)
                    projectile.Value.Game = this;
                foreach (var healOrbSpawn in HealSpawns)
                    healOrbSpawn.Game = this;
                ClonerSpawn.Game = this;

            }
        }

        public bool HasUser(TUser user) => _users.ContainsKey(user);

        protected virtual void OnActionExecuted(TUser from, GameAction action)
        {
        }

        protected virtual void OnTurnEnded()
        {
        }

        protected virtual void OnGameEnd()
        {
        }

        protected abstract void OnIllegalAction(TUser user, string warningMsg);

        protected abstract bool BeforeHandleAction(TUser from, GameAction action);
    }
}