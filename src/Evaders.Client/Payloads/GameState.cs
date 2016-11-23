namespace Evaders.Client.Payloads
{
    using Newtonsoft.Json;

    internal class GameState
    {
        [JsonProperty] public readonly long GameIdentifier;

        [JsonProperty] public readonly ClientGame State;

        [JsonProperty] public readonly long YourIdentifier;

        [JsonConstructor]
        public GameState(long gameIdentifier, ClientGame state, long yourIdentifier)
        {
            GameIdentifier = gameIdentifier;
            State = state;
            YourIdentifier = yourIdentifier;
        }
    }
}