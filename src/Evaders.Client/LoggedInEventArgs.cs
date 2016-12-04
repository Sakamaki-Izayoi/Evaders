namespace Evaders.Client
{
    using System;

    public class LoggedInEventArgs : EventArgs
    {
        public readonly string[] GameModes;
        public readonly string Motd;
        public readonly IQueuer Queuer;

        public LoggedInEventArgs(IQueuer queuer, string motd, string[] gameModes)
        {
            Queuer = queuer;
            Motd = motd;
            GameModes = gameModes;
        }
    }
}