namespace Evaders.CommonNetworking.CommonPayloads
{
    using Newtonsoft.Json;

    public class QueueAction
    {
        [JsonProperty] public readonly string GameMode;

        [JsonConstructor]
        public QueueAction(string gameMode)
        {
            GameMode = gameMode;
        }
    }
}