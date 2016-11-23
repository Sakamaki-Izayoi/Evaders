namespace Evaders.CommonNetworking.CommonPayloads
{
    using Newtonsoft.Json;

    public class IllegalAction
    {
        [JsonProperty] public readonly long? GameIdentifier;

        [JsonProperty] public readonly bool InsideGame;

        [JsonProperty] public readonly string Message;

        [JsonConstructor]
        public IllegalAction(string message, bool insideGame, long? gameIdentifier)
        {
            Message = message;
            InsideGame = insideGame;
            GameIdentifier = gameIdentifier;
        }
    }
}