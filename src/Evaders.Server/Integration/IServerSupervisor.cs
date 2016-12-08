namespace Evaders.Server.Integration
{
    using System;
    using System.Collections.Generic;
    using Core.Game;

    public interface IServerSupervisor
    {
        bool IsRecording { get; }

        void GameEndedTurn(ServerGame game, GameChangeTracker tracker);
        void GameEnded(ServerGame game, Guid winnersIdentifiers, Guid[] loosersIdentifier);
        Guid GetBestChoice(Guid player, IEnumerable<Guid> possibleOpponents);
    }
}