namespace Evaders.CommonNetworking.CommonPayloads
{
    using Newtonsoft.Json;

    public class IllegalAction
    {
        [JsonProperty] public readonly bool InsideGame;

        [JsonProperty] public readonly string Message;

        [JsonConstructor]
        public IllegalAction(string message, bool insideGame)
        {
            Message = message;
            InsideGame = insideGame;
        }
    }
}