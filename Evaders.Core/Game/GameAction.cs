namespace Evaders.Core.Game
{
    using Utility;

    public class GameAction
    {
        public readonly long ControlledEntityIdentifier;
        public readonly Vector2 Position;
        public readonly GameActionType Type;

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