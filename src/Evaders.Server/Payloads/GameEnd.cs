namespace Evaders.Server.Payloads
{
    using Newtonsoft.Json;

    internal class GameEnd
    {
        [JsonProperty] public readonly long GameIdentifier;

        [JsonProperty] public readonly IServerUser[] Participants;

        [JsonProperty] public readonly long WinnerIdentifier;

        [JsonProperty] public readonly bool YouWon;

        [JsonConstructor]
        public GameEnd(long gameIdentifier, IServerUser[] participants, bool youWon, long winnerIdentifier)
        {
            GameIdentifier = gameIdentifier;
            Participants = participants;
            YouWon = youWon;
            WinnerIdentifier = winnerIdentifier;
        }
    }
}