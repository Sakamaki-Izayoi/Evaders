namespace Evaders.Core.Game
{
    using System;
    using Newtonsoft.Json;
    using Utility;

    public class EntityBase
    {
        public bool CanShoot => (Game.Turn - LastShotFrame) * Game.TimePerFrameSec >= CharData.ReloadDelaySec;
        public int ReloadFrames => (int)Math.Ceiling(CharData.ReloadDelaySec / Game.TimePerFrameSec);

        [JsonProperty]
        public int Health { get; private set; }

        [JsonProperty]
        public Vector2 Position { get; internal set; }

        public int HitboxSize => CharData.HitboxSize;

        [JsonProperty]
        public long EntityIdentifier { get; }

        [JsonProperty]
        public long PlayerIdentifier { get; }

        [JsonProperty]
        public Vector2 MovingTo { get; private set; }

        [JsonProperty]
        public CharacterData CharData;

        public double MovingDistancePerTurn => CharData.SpeedSec * Game.TimePerFrameSec;
        public Vector2 PositionNextTurn => GetPositionIn(1);

        internal GameBase Game;

        [JsonProperty]
        internal int LastShotFrame = short.MinValue;

        public EntityBase(CharacterData charData, Vector2 position, long playerIdentifier, long entityIdentifier, GameBase game)
        {
            CharData = charData;
            Game = game;
            EntityIdentifier = entityIdentifier;
            Health = CharData.MaxHealth;
            PlayerIdentifier = playerIdentifier;
            Position = position;

            MovingTo = Position;
        }

        [JsonConstructor]
        private EntityBase(CharacterData charData, Vector2 position, long playerIdentifier, long entityIdentifier, Vector2 movingTo)
        {
            CharData = charData;
            EntityIdentifier = entityIdentifier;
            PlayerIdentifier = playerIdentifier;
            Position = position;

            MovingTo = movingTo;
        }

        public void InflictDamage(int amount)
        {
            if ((Health -= amount) <= 0)
                Game.HandleDeath(this);
        }

        public void Update()
        {
            if ((Position - MovingTo).LengthSqr < CharData.SpeedSec * Game.TimePerFrameSec * CharData.SpeedSec * Game.TimePerFrameSec)
                Position = MovingTo;
            else
                Position = Position.Extended(MovingTo, CharData.SpeedSec * Game.TimePerFrameSec);
        }

        /// <summary>
        /// Gets how many turns are needed to reach the given position with a projectile
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public int GetNeededProjectileTurns(Vector2 position)
        {
            var sec = position.Distance(Position.Extended(position, HitboxSize + CharData.ProjectileHitboxSize), true) / (CharData.ProjectileSpeedSec * CharData.ProjectileSpeedSec);
            return (int)Math.Ceiling(sec / Game.TimePerFrameSec);
        }

        internal bool MoveToInternal(Vector2 coord)
        {
            MovingTo = coord;
            return true;
        }

        internal bool ShootInternal(Vector2 coord)
        {
            if (coord.Distance(Position, true) <= float.Epsilon)
                return false;
            if (CanShoot)
            {
                LastShotFrame = Game.Turn;
                Game.SpawnProjectile((coord - Position).Unit, this);
                return true;
            }
            return false;
        }

        public Vector2 GetPositionIn(uint turns)
        {
            return GetPositionIn(turns, MovingTo);
        }

        public Vector2 GetPositionIn(uint turns, Vector2 movingTo)
        {
            var distance = MovingDistancePerTurn * turns;
            return movingTo.Distance(Position, true) <= distance * distance ? movingTo : Position.Extended(movingTo, distance);
        }
    }
}