namespace Evaders.Core.Game
{
    using System;
    using Newtonsoft.Json;
    using Utility;

    public class Projectile<TUser> where TUser : IUser //todo: make non generic IGame interface that supplies the nessecary properties
    {
        [JsonProperty]
        public Vector2 Position { get; internal set; }

        public readonly int Damage;
        public readonly Vector2 Direction;
        public readonly long EntityIdentifier;
        public readonly int HitboxRadius;
        public readonly int LifeEndFrame;
        public readonly long PlayerIdentifier;
        public readonly long ProjectileIdentifier;
        public readonly double ProjectileSpeedSec;

        internal Game<TUser> Game;

        internal Projectile(Vector2 direction, Entity<TUser> entity, Game<TUser> game, long projectileIdentifier, int lifeEndFrame)
        {
            if (!direction.IsUnitVector)
                throw new ArgumentException("Not a direction (unit vector)", nameof(direction));

            Position = entity.Position.Extended(direction, entity.CharData.HitboxSize + entity.CharData.ProjectileHitboxSize);
            Direction = direction;
            HitboxRadius = entity.CharData.ProjectileHitboxSize;
            Damage = entity.CharData.ProjectileDamage;
            PlayerIdentifier = entity.PlayerIdentifier;
            EntityIdentifier = entity.EntityIdentifier;
            ProjectileIdentifier = projectileIdentifier;
            LifeEndFrame = lifeEndFrame;
            ProjectileSpeedSec = entity.CharData.ProjectileSpeedSec;

            Game = game;
        }

        public void Update()
        {
            if (Game.Frame >= LifeEndFrame)
            {
                Game.RemoveAfterFrame(this);
                return;
            }

            Position = Position + Direction*ProjectileSpeedSec*Game.TimePerFrameSec;
            foreach (var entity in Game.Entities)
                if (entity.PlayerIdentifier != PlayerIdentifier && entity.Position.Distance(Position, true) <= (HitboxRadius + entity.CharData.HitboxSize)*(HitboxRadius + entity.CharData.HitboxSize))
                {
                    Game.RemoveAfterFrame(this);
                    if ((entity.Health -= Damage) <= 0)
                        Game.RemoveAfterFrame(entity);
                    break;
                }
        }
    }
}