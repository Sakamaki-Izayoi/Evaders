namespace Evaders.Client
{
    using System;
    using CommonNetworking.CommonPayloads;

    public class LoggedInEventArgs : EventArgs
    {
        public readonly IQueuer Queuer;
        public readonly string Motd;
        public readonly string[] GameModes;

        public LoggedInEventArgs(IQueuer queuer, string motd, string[] gameModes)
        {
            Queuer = queuer;
            Motd = motd;
            GameModes = gameModes;
        }
    }
}