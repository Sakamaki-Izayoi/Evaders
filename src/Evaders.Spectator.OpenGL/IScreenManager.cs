namespace Evaders.Spectator.OpenGL
{
    public enum SeeThrough
    {
        Fully,
        Partial,
        None
    }

    public interface IScreenManager
    {
        void Add(Screen screen);
        void Remove(Screen screen);
    }
}