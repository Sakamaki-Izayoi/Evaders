namespace Evaders.CommonNetworking.CommonPayloads
{
    using Core.Game;
    using Core.Utility;
    using Newtonsoft.Json;

    public class LiveGameAction : GameAction
    {
        [JsonProperty] public readonly long GameIdentifier;

        [JsonConstructor]
        public LiveGameAction(GameActionType type, Vector2 position, long controlledEntityIdentifier, long gameIdentifier) : base(type, position, controlledEntityIdentifier)
        {
            GameIdentifier = gameIdentifier;
        }
    }
}