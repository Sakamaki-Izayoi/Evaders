namespace Evaders.Core.Game
{
    using System;
    using System.Collections.Generic;
    using Newtonsoft.Json;
    using Utility;

    public abstract class GameBase
    {
        public abstract IEnumerable<EntityBase> Entities { get; }
        public abstract IEnumerable<Projectile> Projectiles { get; }
        public abstract IEnumerable<HealOrbSpawn> HealSpawns { get; }
        public abstract double TimePerFrameSec { get; }
        public abstract CloneOrbSpawn ClonerSpawn { get; }
        public double CurrentArenaRadius => GetArenaRadius(Turn);

        [JsonProperty]
        public int Turn { get; protected set; }

        [JsonProperty]
        public readonly GameSettings Settings;

        protected GameBase(GameSettings settings)
        {
            Settings = settings;
        }

        public double GetArenaRadius(int turn) => turn < Settings.ArenaShrinkStartTurn ? Settings.ArenaRadius : (float)Math.Max(0f, Settings.ArenaRadius - Settings.ArenaShrinkPerSec * (turn + 1 - Settings.ArenaShrinkStartTurn) * TimePerFrameSec);

        protected internal abstract void HandleDeath(Projectile projectile);
        protected internal abstract void HandleDeath(EntityBase entity);
        protected internal abstract Projectile SpawnProjectile(Vector2 direction, EntityBase entity);
        protected internal abstract Entity SpawnEntity(Vector2 position, long playerIdentifier, CharacterData charData);
        protected internal abstract bool AddAction(long userIdentifier, GameAction action);
        protected internal virtual void HandleEntityHealthChanged(EntityBase entity) { }
        protected internal virtual void HandleOrbChangedState(OrbSpawn spawn) { }
    }
}