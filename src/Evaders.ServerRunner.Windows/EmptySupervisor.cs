namespace Evaders.ServerRunner.Windows
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Core.Game;
    using Server;
    using Server.Integration;

    internal class EmptySupervisor : IServerSupervisor
    {
        public bool IsRecording => false;

        public void GameEndedTurn(ServerGame game, GameChangeTracker tracker)
        {
            
        }

        public void GameEnded(ServerGame game, Guid winnersIdentifiers, Guid[] loosersIdentifier)
        {
        }

        public Guid GetBestChoice(Guid player, IEnumerable<Guid> possibleOpponents)
        {
            return possibleOpponents.First();
        }

    }
}