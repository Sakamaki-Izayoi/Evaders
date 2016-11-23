using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Evaders.Spectator
{
    using Microsoft.Xna.Framework;

    internal static class Extensions
    {
        public static Vector2 AsMonoVector(this Core.Utility.Vector2 vector) => new Vector2((float)vector.X, (float)vector.Y);
    }
}
