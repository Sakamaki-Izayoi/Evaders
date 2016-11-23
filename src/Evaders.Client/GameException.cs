namespace Evaders.Client
{
    using System;

    internal class GameException : Exception
    {
        public GameException(string reason) : base(reason)
        {
        }
    }
}