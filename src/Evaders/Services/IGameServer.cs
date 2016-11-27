namespace Evaders.Services
{
    using System;

    public interface IGameServer : IDisposable
    {
        void Start();
    }
}