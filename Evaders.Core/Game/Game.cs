namespace Evaders.Core.Game
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.Serialization;
    using Newtonsoft.Json;
    using Utility;

    public abstract class Game<TUser> where TUser : IUser
    {
        public float TimePerFrameSec => 1000f/Settings.FramesPerSecond;
        public bool GameEnded => Entities.All(entity => entity.PlayerIdentifier == Entities.FirstOrDefault()?.PlayerIdentifier);
        public IEnumerable<TUser> Users => _users.Keys;
        public IReadOnlyList<Entity<TUser>> ValidEntities => Entities;
        public IReadOnlyList<Projectile<TUser>> ValidProjectiles => Projectiles;
        private readonly long _entityIdentifier;

        private readonly List<Entity<TUser>> _toRemoveEntities = new List<Entity<TUser>>();
        private readonly List<Projectile<TUser>> _toRemoveProjectiles = new List<Projectile<TUser>>();
        private readonly Dictionary<TUser, List<GameAction>> _users;
        [JsonProperty] internal readonly List<Entity<TUser>> Entities = new List<Entity<TUser>>();
        public readonly long Identifier;
        [JsonProperty] internal readonly List<Projectile<TUser>> Projectiles = new List<Projectile<TUser>>();

        public readonly GameSettings Settings;
        private long _projectileIdentifier;
        public int Frame;

        public Game(IEnumerable<TUser> users, GameSettings settings, long gameIdentifier)
        {
            Settings = settings;
            Identifier = gameIdentifier;
            _users = users.ToDictionary(item => item, item => new List<GameAction>());

            var unitUp = new Vector2(0, -1);
            var rotateBy = 360f/_users.Count;
            foreach (var user in _users)
            {
                Entities.Add(new Entity<TUser>(Settings.DefaultCharacterData, unitUp*(Settings.ArenaRadius - Settings.DefaultCharacterData.HitboxSize), user.Key.Identifier, _entityIdentifier++, this));
                unitUp = unitUp.RotatedDegrees(rotateBy);
            }
        }

        public void NextTurn()
        {
            Frame++;

            foreach (var user in _users)
                foreach (var gameAction in user.Value)
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
                            result = controlledEntity.MoveTo(gameAction.Position);
                            break;
                        case GameActionType.Shoot:
                            result = controlledEntity.Shoot(gameAction.Position);
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

            if (GameEnded)
                OnGameEnd();
            else
                OnTurnEnded();
        }

        public void AddAction(TUser from, GameAction action)
        {
            if (!ShouldHandleAction(@from, action))
                return;

            _users[from].Add(action);
        }

        internal void RemoveAfterFrame(Entity<TUser> entity)
        {
            _toRemoveEntities.Add(entity);
        }

        internal void RemoveAfterFrame(Projectile<TUser> projectile)
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

        protected abstract void OnActionExecuted(TUser @from, GameAction action);

        protected abstract void OnTurnEnded();

        protected abstract void OnGameEnd();

        protected abstract void OnIllegalAction(TUser user, string warningMsg);

        protected abstract bool ShouldHandleAction(TUser @from, GameAction action);

        internal long GenerateProjectileIdentifier() => _projectileIdentifier++;
    }
}