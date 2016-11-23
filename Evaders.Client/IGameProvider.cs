namespace Evaders.Client
{
    using System.Collections.Generic;
    using Core.Game;

    public interface IGameProvider
    {
        IReadOnlyDictionary<long, GameBase> RunningGames { get; }
    }
}