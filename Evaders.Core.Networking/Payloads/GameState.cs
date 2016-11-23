namespace Evaders.Server.Payloads
{
    internal class GameState
    {
        public long Identifier;
        public ServerGame State;

        public GameState(long identifier, ServerGame state)
        {
            Identifier = identifier;
            State = state;
        }
    }
}