namespace Evaders.Core.Game
{
    using System;
    using Newtonsoft.Json;
    using Utility;

    public class Entity : EntityBase
    {
        internal Entity(CharacterData charData, Vector2 position, long playerIdentifier, long entityIdentifier, GameBase game) : base(charData, position, playerIdentifier, entityIdentifier, game)
        {
        }

        [JsonConstructor]
        protected Entity(CharacterData charData) : base(charData)
        {
        }

        public void MoveTo(Vector2 coord)
        {
            Game.AddAction(PlayerIdentifier, new GameAction(GameActionType.Move, coord, EntityIdentifier));
        }

        public void Shoot(Vector2 coord)
        {
            Game.AddAction(PlayerIdentifier, new GameAction(GameActionType.Shoot, coord, EntityIdentifier));
        }

        public void ShootDirection(Vector2 direction)
        {
            if (!direction.IsUnitVector)
                throw new ArgumentException("The given direction is not a direction (unit vector)", nameof(direction));

            Game.AddAction(PlayerIdentifier, new GameAction(GameActionType.Shoot, Position + direction, EntityIdentifier));
        }
    }
}