namespace Evaders.Core.Game
{
    using System.Collections.Generic;
    using Utility;

    public class DefaultMapGenerator : IMapGenerator
    {
        public IEnumerable<Vector2> GetEntityPositions(int entityCount, GameSettings settings)
        {
            var unitUp = new Vector2(0, -1);
            var rotateBy = 360f/entityCount;
            for (var i = 0; i < entityCount; i++)
            {
                yield return unitUp*(settings.ArenaRadius - settings.DefaultCharacterData.HitboxSize);
                unitUp = unitUp.RotatedDegrees(rotateBy);
            }
        }

        public IEnumerable<Vector2> GetHealorbPositions(int entityCount, GameSettings settings)
        {
            entityCount *= 3;
            var unitUp = new Vector2(0, -1);
            var rotateBy = 360f/entityCount;
            for (var i = 0; i < entityCount; i++)
            {
                yield return unitUp*(settings.ArenaRadius/2 - settings.DefaultCharacterData.HitboxSize);
                unitUp = unitUp.RotatedDegrees(rotateBy);
            }
        }
    }
}