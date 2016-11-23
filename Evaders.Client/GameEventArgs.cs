namespace Evaders.Client
{
    using System;

    public class GameEventArgs : EventArgs
    {
        public readonly ClientGame Game;

        public GameEventArgs(ClientGame game)
        {
            Game = game;
        }
    }
}