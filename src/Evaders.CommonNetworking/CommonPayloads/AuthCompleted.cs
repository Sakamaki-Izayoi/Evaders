namespace Evaders.CommonNetworking.CommonPayloads
{
    using Newtonsoft.Json;

    public class AuthCompleted
    {
        [JsonProperty]
        public readonly string[] GameModes;

        [JsonProperty]
        public readonly string Motd;

        [JsonProperty]
        public readonly int MaxQueueCount;

        [JsonConstructor]
        public AuthCompleted(string motd, string[] gameModes, int maxQueueCount)
        {
            Motd = motd;
            GameModes = gameModes;
            MaxQueueCount = maxQueueCount;
        }
    }
}