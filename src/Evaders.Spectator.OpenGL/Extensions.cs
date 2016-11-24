namespace Evaders.Spectator.DirectX
{
    using Microsoft.Xna.Framework;

    internal static class Extensions
    {
        public static Vector2 AsMonoVector(this Core.Utility.Vector2 vector) => new Vector2((float) vector.X, (float) vector.Y);
    }
}