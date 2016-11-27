namespace Evaders.Core.Game
{
    using System.Collections.Generic;
    using Newtonsoft.Json;
    using Utility;

    public abstract class GameBase
    {
        public abstract IEnumerable<EntityBase> ValidEntities { get; }
        public abstract IEnumerable<Projectile> ValidProjectiles { get; }
        public abstract double TimePerFrameSec { get; }

        [JsonProperty]
        public int Turn { get; protected set; }

        [JsonProperty] public readonly GameSettings Settings;

        protected GameBase(GameSettings settings)
        {
            Settings = settings;
        }

        protected internal abstract void HandleDeath(Projectile projectile);
        protected internal abstract void HandleDeath(EntityBase entity);
        protected internal abstract void SpawnProjectile(Vector2 direction, EntityBase entity);
        protected internal abstract bool AddAction(long userIdentifier, GameAction action);
    }
}