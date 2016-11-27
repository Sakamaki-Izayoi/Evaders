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
        public readonly ServerUser Winner;
        public readonly bool YouWon;

        [JsonConstructor]
        public GameEnd(long gameIdentifier, bool youWon, ServerUser winner, ServerUser[] participants)
        {
            GameIdentifier = gameIdentifier;
            YouWon = youWon;
            Winner = winner;
            Participants = participants;
        }
    }
}