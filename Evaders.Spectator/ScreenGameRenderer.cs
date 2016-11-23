using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Evaders.Spectator
{
    using Client;
    using Core.Game;
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;
    using Microsoft.Xna.Framework.Input;

    public class ScreenGameRenderer : Screen
    {
        private readonly IGameProvider _games;
        private Vector2 _cameraPosition = Vector2.Zero;
        private float _zoom = 0.2f;
        private const float MaxZoom = 2f, MinZoom = 0.2f;
        private Matrix ViewMatrix => Matrix.CreateScale(0.2f, 0.2f, 1f);// * Matrix.CreateTranslation(_cameraPosition.X, _cameraPosition.Y, 0f);
        private MouseState _lastMouseState;
        private static readonly Color[] PlayerColorArray = { Color.DarkRed, Color.CornflowerBlue, Color.Goldenrod, Color.White, Color.Purple, Color.Chocolate, Color.OrangeRed, Color.Honeydew };
        private readonly Dictionary<long, int> _playerColorMapper = new Dictionary<long, int>();
        private long _gameViewIdentifier;
        private KeyboardState _lastKeyboardState;
        private bool _firstUpdate;
        public override Color BackgroundColor => Color.Red;

        public ScreenGameRenderer(IScreenManager manager, IGameProvider games) : base(manager)
        {
            _games = games;
        }

        public override void Draw(SpriteBatch spritebatch, GraphicsDeviceManager graphicsDeviceManager)
        {
            if (!_games.RunningGames.Any())
                return;

            if (!_games.RunningGames.ContainsKey(_gameViewIdentifier))
            {
                _gameViewIdentifier = _games.RunningGames.First().Key;
            }
            var game = _games.RunningGames[_gameViewIdentifier];

            spritebatch.Begin(/*transformMatrix: ViewMatrix, */blendState: BlendState.AlphaBlend, samplerState: SamplerState.LinearClamp);

            foreach (var validEntity in game.ValidEntities)
            {
                if (!_playerColorMapper.ContainsKey(validEntity.PlayerIdentifier))
                    _playerColorMapper.Add(validEntity.PlayerIdentifier, _playerColorMapper.Count);
                var entityColor = PlayerColorArray[_playerColorMapper[validEntity.PlayerIdentifier]];
                var outlineColor = validEntity.CanShoot ? Color.Green : Color.Red;
                const float outlineFactor = 1.2f;

                var destination = new Rectangle((int)validEntity.Position.X, (int)validEntity.Position.Y, validEntity.HitboxSize * 2, validEntity.HitboxSize * 2);

                var newDiameter = destination.Width * outlineFactor;
                var destTransformed = new Rectangle((int)(destination.X), (int)(destination.Y), (int)newDiameter, (int)newDiameter);
                destination.Y += 950;
                destTransformed.Y += 950;
            //    spritebatch.Draw(TextureManager.Get(Texture.Circle), destinationRectangle: destTransformed, color: outlineColor, origin: new Vector2(destTransformed.Width / 2f, destTransformed.Height / 2f));
                Console.WriteLine("REN1 " + destTransformed);
              //  spritebatch.Draw(TextureManager.Get(Texture.Circle), destinationRectangle: destination, color: entityColor, origin: new Vector2(destination.Width / 2f, destination.Height / 2f));
                Console.WriteLine("REN2 " + destination);
                spritebatch.Draw(TextureManager.Get(Texture.Pixel), destinationRectangle: destTransformed, color: Color.White, origin: new Vector2(destTransformed.Width / 2f, destTransformed.Height / 2f));
                Console.WriteLine("REN3 " + destTransformed);
                spritebatch.Draw(TextureManager.Get(Texture.Pixel), destinationRectangle: destination, color: Color.Beige, origin: new Vector2(destination.Width / 2f, destination.Height / 2f));
                Console.WriteLine("REN4 " + destination);

            }

            foreach (var projectile in game.ValidProjectiles)
            {
                if (!_playerColorMapper.ContainsKey(projectile.PlayerIdentifier))
                    _playerColorMapper.Add(projectile.PlayerIdentifier, _playerColorMapper.Count);
                var entityColor = PlayerColorArray[_playerColorMapper[projectile.PlayerIdentifier]];

                spritebatch.Draw(TextureManager.Get(Texture.Circle), destinationRectangle: new Rectangle((int)projectile.Position.X, (int)projectile.Position.Y, projectile.HitboxRadius * 2, projectile.HitboxRadius * 2), color: entityColor, origin: new Vector2(projectile.HitboxRadius, projectile.HitboxRadius));
            }

            spritebatch.End();
        }

        public override void Update(double deltaT, GraphicsDeviceManager graphicsDeviceManager)
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
                var scale = (mouseState.ScrollWheelValue - _lastMouseState.ScrollWheelValue < 0 ? 0.95f : 1.05f);
                _zoom *= scale;// Matrix.CreateTranslation(-graphicsDeviceManager.PreferredBackBufferWidth / 2f, -graphicsDeviceManager.PreferredBackBufferHeight / 2f, 0) * Matrix.CreateScale(scale, scale, 1f) * Matrix.CreateTranslation(graphicsDeviceManager.PreferredBackBufferWidth / 2f, graphicsDeviceManager.PreferredBackBufferHeight / 2f, 0);
            }

            if (_lastMouseState.LeftButton == ButtonState.Pressed && mouseState.LeftButton == ButtonState.Pressed)
            {
                _cameraPosition += new Vector2((mouseState.X - _lastMouseState.X) * 10, (mouseState.Y - _lastMouseState.Y) * 10);
                //   _viewMatrix *= Matrix.CreateTranslation(mouseState.X - _lastMouseState.X, mouseState.Y - _lastMouseState.Y, 0);
            }

            if (keyboardState.IsKeyUp(Keys.Space) && _lastKeyboardState.IsKeyDown(Keys.Space))
            {
                var index = _games.RunningGames.Keys.ToList().IndexOf(_gameViewIdentifier);
                if (index != -1)
                    _gameViewIdentifier = _games.RunningGames.Keys.ToArray()[++index % _games.RunningGames.Count];
            }

            _lastKeyboardState = keyboardState;
            _lastMouseState = mouseState;
        }
    }
}
