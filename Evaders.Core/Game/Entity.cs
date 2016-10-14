namespace Evaders.Core.Game
{
    using System;
    using Newtonsoft.Json;
    using Utility;

    public class Entity<TUser> where TUser : IUser
    {
        public bool CanShoot => (Game.Frame - LastShotFrame)*Game.TimePerFrameSec >= CharData.ReloadDelaySec;
        public int ReloadFrames => (int) Math.Ceiling(CharData.ReloadDelaySec/Game.TimePerFrameSec);
        public int Health { get; internal set; }

        [JsonProperty]
        public Vector2 Position { get; internal set; }

        public readonly CharacterData CharData;
        public readonly long EntityIdentifier;
        public readonly long PlayerIdentifier;
        private Vector2 _movingTo;
        internal Game<TUser> Game;

        [JsonProperty] internal int LastShotFrame = short.MinValue;

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
                Position = Position.Extended(_movingTo, CharData.SpeedSec*Game.TimePerFrameSec);
        }

        internal bool MoveToInternal(Vector2 coord)
        {
            _movingTo = coord;
            return true;
        }

        internal bool ShootInternal(Vector2 coord)
        {
            if (coord.Distance(Position, true) <= float.Epsilon)
                return false;
            if (CanShoot)
            {
                LastShotFrame = Game.Frame;
                var projectile = new Projectile<TUser>((coord - Position).Unit, this, Game, Game.GenerateProjectileIdentifier(), Game.Frame + (int) Math.Ceiling(Game.Settings.ProjectileLifeTimeSec/Game.TimePerFrameSec));
                Game.Projectiles.Add(projectile);
                return true;
            }
            return false;
        }

        public void MoveTo(Vector2 coord)
        {
            Game.AddAction(Game.GetUser(PlayerIdentifier), new GameAction(GameActionType.Move, coord, EntityIdentifier));
        }

        public void Shoot(Vector2 coord)
        {
            Game.AddAction(Game.GetUser(PlayerIdentifier), new GameAction(GameActionType.Shoot, coord, EntityIdentifier));
        }

        public void ShootDirection(Vector2 direction)
        {
            if (!direction.IsUnitVector)
                throw new ArgumentException("The given direction is not a direction (unit vector)", nameof(direction));

            Game.AddAction(Game.GetUser(PlayerIdentifier), new GameAction(GameActionType.Shoot, Position + direction, EntityIdentifier));
        }
    }
}