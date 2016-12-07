namespace Evaders.Core.Game
{
    using System.Collections.Generic;
    using Utility;

    public interface IMapGenerator
    {
        IEnumerable<Vector2> GetEntityPositions(int entityCount, GameSettings settings);
        IEnumerable<Vector2> GetHealorbPositions(int entityCount, GameSettings settings);
    }
}