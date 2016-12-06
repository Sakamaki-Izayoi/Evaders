namespace Evaders.Spectator.OpenGL
{
    using System;
    using System.Linq;
    using Microsoft.Xna.Framework.Content;
    using Microsoft.Xna.Framework.Graphics;

    public enum Texture
    {
        Circle,
        Pixel,
        Star
    }

    public static class TextureManager
    {
        private static Texture2D[] _textures;
        private static bool _loaded;

        public static void LoadContent(ContentManager content)
        {
            if (_loaded)
                return;
            _loaded = true;

            var textures = Enum.GetNames(typeof(Texture)).ToArray();
            _textures = new Texture2D[textures.Length];

            for (var index = 0; index < textures.Length; index++)
                _textures[index] = content.Load<Texture2D>(textures[index]);
        }

        public static Texture2D Get(Texture tex) => _textures[(int) tex];
        public static Texture2D Get(int i) => _textures[i];
    }
}