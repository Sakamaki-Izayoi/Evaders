namespace Evaders.Spectator.OpenGL
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Client;
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;
    using Microsoft.Xna.Framework.Input;

    public class ScreenGameRenderer : Screen
    {
        public override Color BackgroundColor => new Color(60, 0, 0);
        private const float MaxZoom = 2f, MinZoom = 0.2f;
        private static readonly Color[] PlayerColorArray = {Color.DarkRed, Color.CornflowerBlue, Color.Goldenrod, Color.White, Color.Purple, Color.Chocolate, Color.OrangeRed, Color.Honeydew};
        private readonly Connection _connection;
        private readonly Dictionary<long, int> _playerColorMapper = new Dictionary<long, int>();
        private Vector2 _cameraPosition = Vector2.Zero;
        private bool _firstUpdate;
        private KeyboardState _lastKeyboardState;
        private MouseState _lastMouseState;
        private float _zoom = 0.2f;

        public ScreenGameRenderer(IScreenManager manager, Connection connection) : base(manager)
        {
            _connection = connection;
        }

        public override void Draw(SpriteBatch spritebatch, GraphicsDeviceManager graphicsDeviceManager)
        {
            if (_connection.Game == null)
                return;

            var game = _connection.Game;

            var viewMatrix = Matrix.CreateTranslation(_cameraPosition.X, _cameraPosition.Y, 0f)*Matrix.CreateScale(_zoom, _zoom, 1f)*Matrix.CreateTranslation(graphicsDeviceManager.PreferredBackBufferWidth/2f, graphicsDeviceManager.PreferredBackBufferHeight/2f, 0f);


            spritebatch.Begin(transformMatrix: viewMatrix, blendState: BlendState.AlphaBlend, samplerState: SamplerState.LinearClamp);
            DrawCircle(spritebatch, Vector2.Zero, game.CurrentArenaRadius, Color.White);

            foreach (var gameHealSpawn in game.HealSpawns.Where(item => item.IsUp))
                DrawCentered(Texture.Star, spritebatch, gameHealSpawn.Position, gameHealSpawn.HitboxSize, Color.Green);

            DrawCentered(Texture.Star, spritebatch, game.ClonerSpawn.Position, game.ClonerSpawn.HitboxSize, Color.OrangeRed);

            foreach (var validEntity in game.Entities)
            {
                if (!_playerColorMapper.ContainsKey(validEntity.PlayerIdentifier))
                    _playerColorMapper.Add(validEntity.PlayerIdentifier, _playerColorMapper.Count);
                var entityColor = PlayerColorArray[_playerColorMapper[validEntity.PlayerIdentifier]];
                var outlineColor = validEntity.CanShoot ? Color.Green : Color.Red;
                const float outlineFactor = 0.8f;

                var entityPos = DrawCircle(spritebatch, validEntity.Position, validEntity.HitboxSize, outlineColor);
                DrawCircle(spritebatch, validEntity.Position, validEntity.HitboxSize*outlineFactor, entityColor);

                entityPos.Y -= entityPos.Height/3;
                entityPos.Height /= 5;

                var outline = entityPos;
                outline.X -= 5;
                outline.Y -= 5;
                outline.Width += 10;
                outline.Height += 10;

                spritebatch.Draw(TextureManager.Get(Texture.Pixel), outline, Color.Black);

                var hpPercent = 1d - Math.Min(1, validEntity.Health/(double) validEntity.CharData.MaxHealth);
                var barSmallerNum = (int) (entityPos.Width*hpPercent);
                entityPos.Width -= barSmallerNum;

                spritebatch.Draw(TextureManager.Get(Texture.Pixel), entityPos, Color.Green);
            }

            foreach (var projectile in game.Projectiles)
            {
                if (!_playerColorMapper.ContainsKey(projectile.PlayerIdentifier))
                    _playerColorMapper.Add(projectile.PlayerIdentifier, _playerColorMapper.Count);
                var entityColor = PlayerColorArray[_playerColorMapper[projectile.PlayerIdentifier]];

                DrawCircle(spritebatch, projectile.Position, projectile.HitboxSize, entityColor);
                //spritebatch.Draw(TextureManager.Get(Texture.Circle), destinationRectangle: new Rectangle((int)projectile.Position.X, (int)projectile.Position.Y, projectile.HitboxSize * 2, projectile.HitboxSize * 2), color: entityColor, origin: new Vector2(projectile.HitboxSize, projectile.HitboxSize));
            }

            spritebatch.End();
        }

        private Rectangle DrawCircle(SpriteBatch spriteBatch, Core.Utility.Vector2 position, double radius, Color color)
        {
            return DrawCentered(Texture.Circle, spriteBatch, position, radius, color);
        }

        private Rectangle DrawCentered(Texture texture, SpriteBatch spriteBatch, Core.Utility.Vector2 position, double radius, Color color)
        {
            var rect = new Rectangle((int) (position.X - radius), (int) (position.Y - radius), (int) (radius*2), (int) (radius*2));
            spriteBatch.Draw(TextureManager.Get(texture), rect, color);
            return rect;
        }

        private Rectangle DrawCircle(SpriteBatch spriteBatch, Vector2 position, double radius, Color color)
        {
            return DrawCircle(spriteBatch, new Core.Utility.Vector2(position.X, position.Y), radius, color);
        }

        public override void UpdateActive(double deltaT, GraphicsDeviceManager graphicsDeviceManager)
        {
            if (!_firstUpdate)
            {
                _lastKeyboardState = Keyboard.GetState();
                _lastMouseState = Mouse.GetState();
                _firstUpdate = true;
                return;
            }

            var mouseState = Mouse.GetState();
            var keyboardState = Keyboard.GetState();
            if (mouseState.ScrollWheelValue != _lastMouseState.ScrollWheelValue)
            {
                var scale = mouseState.ScrollWheelValue - _lastMouseState.ScrollWheelValue < 0 ? 0.95f : 1.05f;
                _zoom = MathHelper.Clamp(_zoom*scale, MinZoom, MaxZoom);
            }

            if ((_lastMouseState.LeftButton == ButtonState.Pressed) && (mouseState.LeftButton == ButtonState.Pressed))
            {
                var zoomFac = 1f/_zoom;
                _cameraPosition += new Vector2((mouseState.X - _lastMouseState.X)*zoomFac, (mouseState.Y - _lastMouseState.Y)*zoomFac);
            }

            _lastKeyboardState = keyboardState;
            _lastMouseState = mouseState;
        }
    }
}