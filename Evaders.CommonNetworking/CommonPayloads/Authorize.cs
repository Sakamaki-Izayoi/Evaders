namespace Evaders.CommonNetworking.CommonPayloads
{
    using System;
    using Newtonsoft.Json;

    public class Authorize
    {
        [JsonProperty] public readonly bool FullGameState;

        [JsonProperty] public readonly Guid Identifier;

        [JsonProperty] public readonly string Name;

        [JsonConstructor]
        public Authorize(bool fullGameState, Guid identifier, string name)
        {
            FullGameState = fullGameState;
            Identifier = identifier;
            Name = name;
        }

        public Authorize(Guid identifier, string name)
        {
            Identifier = identifier;
            Name = name;
        }
    }
}