namespace Evaders.Client.Payloads
{
    using Newtonsoft.Json;

    internal class GameEnd
    {
        internal class ServerUser
        {
            public readonly long Identifier;
            public readonly bool IsBot;
            public readonly string Username;

            [JsonConstructor]
            public ServerUser(long identifier, string username, bool isBot)
            {
                Identifier = identifier;
                Username = username;
                IsBot = isBot;
            }
        }

        public readonly long GameIdentifier;
        public readonly ServerUser[] Participants;
        public readonly long WinnerIdentifier;
        public readonly bool YouWon;

        [JsonConstructor]
        public GameEnd(long gameIdentifier, bool youWon, long winnerIdentifier, ServerUser[] participants)
        {
            GameIdentifier = gameIdentifier;
            YouWon = youWon;
            WinnerIdentifier = winnerIdentifier;
            Participants = participants;
        }
    }
}