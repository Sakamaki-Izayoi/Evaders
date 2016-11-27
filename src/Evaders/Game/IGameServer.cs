namespace Evaders.Game
{
    using System;

    public interface IGameServer : IDisposable
    {
        void Start();

        void Stop();
    }
}