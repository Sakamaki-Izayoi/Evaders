namespace Evaders.Core.Game
{
    using Utility;

    public interface IGameSupervisor<TUser> where TUser : IUser
    {
        bool ShouldHandleAction(Game<TUser> game, TUser from, GameAction action);
        void OnGameAction(Game<TUser> game, GameActionType action, Vector2 actionPosition);
        void OnGameEnd(Game<TUser> game);
        void OnIllegalAction(Game<TUser> game, TUser user, string message);
        void OnTurnEnded(Game<TUser> game);
    }
}