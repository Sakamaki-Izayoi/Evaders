namespace Evaders.Spectator.OpenGL
{
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;

    public abstract class Screen
    {
        public virtual SeeThrough SeeThroughType => SeeThrough.None;
        public virtual Color BackgroundColor => Color.Black;
        private readonly IScreenManager _manager;

        protected Screen(IScreenManager manager)
        {
            _manager = manager;
        }

        public abstract void Draw(SpriteBatch spritebatch, GraphicsDeviceManager graphicsDeviceManager);
        public abstract void UpdateActive(double deltaT, GraphicsDeviceManager graphicsDeviceManager);

        public virtual void Resize(GraphicsDeviceManager graphicsDeviceManager)
        {
        }

        protected void Close()
        {
            _manager.Remove(this);
        }

        protected void AddChild(Screen screen)
        {
            _manager.Add(screen);
        }
    }
}