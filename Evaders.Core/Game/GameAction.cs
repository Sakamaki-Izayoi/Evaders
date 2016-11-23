namespace Evaders.Core.Game
{
    using Newtonsoft.Json;
    using Utility;

    public class GameAction
    {
        [JsonProperty] public readonly long ControlledEntityIdentifier;

        [JsonProperty] public readonly Vector2 Position;

        [JsonProperty] public readonly GameActionType Type;

        [JsonConstructor]
        public GameAction(GameActionType type, Vector2 position, long controlledEntityIdentifier)
        {
            Type = type;
            Position = position;
            ControlledEntityIdentifier = controlledEntityIdentifier;
        }

        public override string ToString()
        {
            return $"{{[{ControlledEntityIdentifier}] {Type}: {Position}}}";
        }
    }
}