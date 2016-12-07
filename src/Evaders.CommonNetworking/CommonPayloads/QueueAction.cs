namespace Evaders.CommonNetworking.CommonPayloads
{
    using Newtonsoft.Json;

    public class QueueAction
    {
        [JsonProperty] public readonly int Count;

        [JsonProperty] public readonly string GameMode;

        [JsonConstructor]
        public QueueAction(string gameMode, int count)
        {
            GameMode = gameMode;
            Count = count;
        }
    }
}