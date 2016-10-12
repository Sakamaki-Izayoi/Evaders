namespace Evaders.Core.Game
{
    using Utility;

    public class Projectile<TUser> where TUser : IUser //todo: make non generic IGame interface that supplies the nessecary properties
    {
        public readonly float Damage;
        public readonly long EntityIdentifier;
        public readonly float HitboxRadius;
        public readonly Vector2 MovingTo;
        public readonly long PlayerIdentifier;
        public readonly long ProjectileIdentifier;
        public readonly float ProjectileSpeedSec;

        internal Game<TUser> Game;
        public Vector2 Position;

        internal Projectile(Vector2 movingTo, Entity<TUser> entity, Game<TUser> game, long projectileIdentifier)
        {
            Position = entity.Position.Extended(movingTo, entity.CharData.HitboxSize + entity.CharData.ProjectileHitboxSize);
            MovingTo = movingTo;
            HitboxRadius = entity.CharData.ProjectileHitboxSize;
            Damage = entity.CharData.ProjectileDamage;
            PlayerIdentifier = entity.PlayerIdentifier;
            EntityIdentifier = entity.EntityIdentifier;
            ProjectileIdentifier = projectileIdentifier;
            ProjectileSpeedSec = entity.CharData.ProjectileSpeedSec;

            Game = game;
        }

        public void Update()
        {
            Position.Extend(MovingTo, ProjectileSpeedSec*Game.TimePerFrameSec);
            foreach (var entity in Game.Entities)
                if (entity.PlayerIdentifier != PlayerIdentifier && entity.Position.Distance(Position, true) < (HitboxRadius + entity.CharData.HitboxSize)*(HitboxRadius + entity.CharData.HitboxSize))
                {
                    Game.RemoveAfterFrame(this);
                    if ((entity.Health -= Damage) <= 0)
                        Game.RemoveAfterFrame(entity);
                    break;
                }
        }
    }
}