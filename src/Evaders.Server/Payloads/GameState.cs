namespace Evaders.Server.Payloads
{
    using Newtonsoft.Json;

    internal class GameState
    {
        [JsonProperty] public readonly long GameIdentifier;

        [JsonProperty] public readonly ServerGame State;

        [JsonProperty] public readonly long YourIdentifier;

        [JsonConstructor]
        public GameState(long gameIdentifier, ServerGame state, long yourIdentifier)
        {
            GameIdentifier = gameIdentifier;
            State = state;
            YourIdentifier = yourIdentifier;
        }
    }
}