namespace Evaders.Core.Game
{
    using Newtonsoft.Json;
    using Utility;

    public class Entity<TUser> where TUser : IUser
    {
        public readonly CharacterData CharData;
        public readonly long EntityIdentifier;
        public readonly long PlayerIdentifier;

        private Vector2 _movingTo;
        internal Game<TUser> Game;
        public float Health;

        [JsonProperty] internal int LastShotFrame;

        public Vector2 Position;

        public Entity(CharacterData charData, Vector2 position, long playerIdentifier, long entityIdentifier, Game<TUser> game)
        {
            CharData = charData;
            EntityIdentifier = entityIdentifier;
            Health = charData.MaxHealth;
            PlayerIdentifier = playerIdentifier;
            Position = position;

            _movingTo = Position;
            Game = game;
        }

        public void Update()
        {
            if ((Position - _movingTo).LengthSqr < CharData.SpeedSec*Game.TimePerFrameSec*CharData.SpeedSec*Game.TimePerFrameSec)
                Position = _movingTo;
            else
                Position.Extend(_movingTo, CharData.SpeedSec*Game.TimePerFrameSec);
        }

        internal bool MoveTo(Vector2 coord)
        {
            _movingTo = coord;
            return true;
        }

        public bool Shoot(Vector2 coord)
        {
            if ((Game.Frame - LastShotFrame)*Game.TimePerFrameSec >= CharData.ReloadDelaySec)
            {
                LastShotFrame = Game.Frame;
                var projectile = new Projectile<TUser>(coord, this, Game, Game.GenerateProjectileIdentifier());
                Game.Projectiles.Add(projectile);
                return true;
            }
            return false;
        }
    }
}