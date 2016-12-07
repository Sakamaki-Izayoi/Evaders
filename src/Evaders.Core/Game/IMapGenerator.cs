using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Evaders.Core.Game
{
    using Utility;

    public interface IMapGenerator
    {
        IEnumerable<Vector2> GetEntityPositions(int entityCount, GameSettings settings);
        IEnumerable<Vector2> GetHealorbPositions(int entityCount, GameSettings settings);
    }
}
