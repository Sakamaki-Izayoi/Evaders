namespace Evaders.Spectator
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;
    using Microsoft.Xna.Framework.Input;

    /// <summary>
    ///     This is the main type for your game.
    /// </summary>
    public class SpectatorWindow : Game, IScreenManager
    {
        public event Action PostUpdate;
        private readonly GraphicsDeviceManager _graphics;
        private readonly Stack<Screen> _screens = new Stack<Screen>();
        private SpriteBatch _spriteBatch;

        public SpectatorWindow()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
        }

        public void Add(Screen screen)
        {
            _screens.Push(screen);
        }

        public void Remove(Screen screen)
        {
            while (_screens.Count > 1 && _screens.Pop() != screen) { }
        }

        [STAThread]
        private static void Main()
        {
            using (var game = new SpectatorWindow())
            {
                game.Add(new ScreenMainMenu(game));
                game.Run();
            }
        }

        /// <summary>
        ///     Allows the game to perform any initialization it needs to before starting to run.
        ///     This is where it can query for any required services and load any non-graphic
        ///     related content.  Calling base.Initialize will enumerate through any components
        ///     and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            // TODO: Add your initialization logic here

            base.Initialize();
        }

        /// <summary>
        ///     LoadContent will be called once per game and is the place to load
        ///     all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            Window.AllowAltF4 = true;
            Window.AllowUserResizing = true;
            Window.Title = Assembly.GetAssembly(GetType()).GetName().Name;

            TextureManager.LoadContent(Content);
        }

        /// <summary>
        ///     UnloadContent will be called once per game and is the place to unload
        ///     game-specific content.
        /// </summary>
        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
        }

        /// <summary>
        ///     Allows the game to run logic such as updating the world,
        ///     checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            if (!IsActive)
                return;
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
            {
                if (_screens.Count <= 1)
                    Exit();
                else
                {
                    _screens.Pop();
                }
            }


            UpdateTopScreenRecursive(gameTime.ElapsedGameTime.TotalSeconds);

            base.Update(gameTime);
            PostUpdate?.Invoke();
        }

        /// <summary>
        ///     This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            DrawTopScreenRecursive();

            base.Draw(gameTime);
        }

        private void UpdateTopScreenRecursive(double deltaT)
        {
            if (_screens.Count == 0)
                return;

            var screen = _screens.Pop();
            if (screen.SeeThroughType != SeeThrough.None)
                UpdateTopScreenRecursive(deltaT);

            _screens.Push(screen);
            screen.Update(deltaT, _graphics);
        }

        private void DrawTopScreenRecursive()
        {
            if (_screens.Count == 0)
                return;

            var screen = _screens.Pop();
            if (screen.SeeThroughType != SeeThrough.None)
                DrawTopScreenRecursive();
            else
            {
                GraphicsDevice.Clear(screen.BackgroundColor);
            }

            if (screen.SeeThroughType == SeeThrough.Partial)
            {
                var pixel = TextureManager.Get(Texture.Pixel);
                _spriteBatch.Begin(blendState: BlendState.Additive);
                _spriteBatch.Draw(pixel, destinationRectangle: new Rectangle(0, 0, _graphics.PreferredBackBufferWidth, _graphics.PreferredBackBufferHeight), color: screen.BackgroundColor);
                _spriteBatch.End();
            }

            _screens.Push(screen);
            screen.Draw(_spriteBatch, _graphics);
        }
    }
}