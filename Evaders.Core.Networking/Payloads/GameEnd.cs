namespace Evaders.Server.Payloads
{
    internal class GameEnd
    {
        public readonly long GameIdentifier;
        public readonly IServerUser[] Participants;
        public readonly IServerUser Winner;
        public readonly bool YouWon;

        public GameEnd(long gameIdentifier, IServerUser[] participants, bool youWon, IServerUser winner)
        {
            GameIdentifier = gameIdentifier;
            Participants = participants;
            YouWon = youWon;
            Winner = winner;
        }
    }
}