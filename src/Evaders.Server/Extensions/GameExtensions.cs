namespace Evaders.Server.Extensions
{
    using CommonNetworking.CommonPayloads;
    using Core.Game;

    public static class GameExtensions
    {
        public static LiveGameAction AsLiveAction(this GameAction action, long gameIdentifier) => new LiveGameAction(action.Type, action.Position, action.ControlledEntityIdentifier, gameIdentifier);
    }
}