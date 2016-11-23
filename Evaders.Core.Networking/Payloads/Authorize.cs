namespace Evaders.Server.Payloads
{
    using System;

    internal class Authorize
    {
        public bool FullGameState;
        public Guid Identifier;
        public string Name;
    }
}