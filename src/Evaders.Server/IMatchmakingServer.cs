namespace Evaders.Server
{
    public interface IMatchmakingServer
    {
        void CreateGame(string gameMode, IMatchmaking source, IServerUser[] users);
    }
}