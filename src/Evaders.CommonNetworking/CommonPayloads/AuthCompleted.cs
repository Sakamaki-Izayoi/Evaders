namespace Evaders.CommonNetworking.CommonPayloads
{
    using Newtonsoft.Json;

    public class AuthCompleted
    {
        [JsonProperty] public readonly string[] GameModes;

        [JsonProperty] public readonly string Motd;

        [JsonConstructor]
        public AuthCompleted(string motd, string[] gameModes)
        {
            Motd = motd;
            GameModes = gameModes;
        }
    }
}