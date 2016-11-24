namespace Evaders.Core.Game
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.Serialization;
    using Newtonsoft.Json;
    using Utility;

    public abstract class Game<TUser> : GameBase where TUser : IUser
    {
        public override double TimePerFrameSec => 1d/Settings.FramesPerSecond;
        public bool GameEnded => Entities.All(entity => entity.PlayerIdentifier == Entities.FirstOrDefault()?.PlayerIdentifier);

        [JsonProperty]
        public IEnumerable<TUser> Users => _users.Keys;

        public override IReadOnlyList<EntityBase> ValidEntities => Entities;
        public override IReadOnlyList<Projectile> ValidProjectiles => Projectiles;
        private readonly long _entityIdentifier;

        private readonly List<Entity> _toRemoveEntities = new List<Entity>();
        private readonly List<Projectile> _toRemoveProjectiles = new List<Projectile>();
        private readonly Dictionary<TUser, List<GameAction>> _users;

        [JsonProperty] protected readonly List<Entity> Entities = new List<Entity>();

        [JsonProperty] protected readonly List<Projectile> Projectiles = new List<Projectile>();

        private long _projectileIdentifier;

        public Game(IEnumerable<TUser> users, GameSettings settings) : base(settings)
        {
            _users = users.ToDictionary(item => item, item => new List<GameAction>());

            var unitUp = new Vector2(0, -1);
            var rotateBy = 360f/_users.Count;
            foreach (var user in _users)
            {
                Entities.Add(new Entity(Settings.DefaultCharacterData, unitUp * (Settings.ArenaRadius - Settings.DefaultCharacterData.HitboxSize), user.Key.Identifier, _entityIdentifier++, this));
                unitUp = unitUp.RotatedDegrees(rotateBy);
            }
        }

        protected void NextTurn()
        {
            foreach (var user in _users)
                foreach (var gameAction in user.Value.OrderByDescending(item => item.Type))
                {
                    var controlledEntity = Entities.FirstOrDefault(item => item.EntityIdentifier == gameAction.ControlledEntityIdentifier);
                    if (controlledEntity == null)
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
                            OnIllegalAction(user.Key, "Unknown Action: " + (int) gameAction.Type);
                            continue;
                    }
                    if (!result)
                        OnIllegalAction(user.Key, "Illegal action: " + gameAction);
                    else
                        OnActionExecuted(user.Key, gameAction);
                }


            foreach (var entity in Entities)
                entity.Update();

            foreach (var projectile in Projectiles)
                projectile.Update();

            foreach (var removeEntity in _toRemoveEntities)
                Entities.Remove(removeEntity);

            foreach (var removeProjectile in _toRemoveProjectiles)
                Projectiles.Remove(removeProjectile);

            foreach (var keyValuePair in _users)
                keyValuePair.Value.Clear();

            Turn++;

            if (GameEnded)
                OnGameEnd();
            else
                OnTurnEnded();
        }

        /// <summary>
        ///     Just adds the action, but does not call any checks like BeforeHandleAction
        /// </summary>
        /// <param name="from"></param>
        /// <param name="action"></param>
        protected void AddActionInternal(TUser from, GameAction action)
        {
            _users[from].Add(action);
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
            if (!BeforeHandleAction(@from, action))
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
            Projectiles.Add(new Projectile(direction.Unit, entity, this, _projectileIdentifier++, Turn + (int) Math.Ceiling(Settings.ProjectileLifeTimeSec/TimePerFrameSec)));
        }

        internal TUser GetUser(long userIdentifier) => _users.Keys.FirstOrDefault(item => item.Identifier == userIdentifier);

        protected internal override void HandleDeath(EntityBase entity)
        {
            _toRemoveEntities.Add((Entity) entity);
        }

        protected internal override void HandleDeath(Projectile projectile)
        {
            _toRemoveProjectiles.Add(projectile);
        }

        [OnDeserialized]
        private void ValidateReferences(StreamingContext context)
        {
            foreach (var entity in Entities)
                entity.Game = this;
            foreach (var projectile in Projectiles)
                projectile.Game = this;
        }

        public bool HasUser(TUser user) => _users.ContainsKey(user);

        protected abstract void OnActionExecuted(TUser @from, GameAction action);

        protected abstract void OnTurnEnded();

        protected abstract void OnGameEnd();

        protected abstract void OnIllegalAction(TUser user, string warningMsg);

        protected abstract bool BeforeHandleAction(TUser @from, GameAction action);
    }
}