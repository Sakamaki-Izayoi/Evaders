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
        private class WinStats : IWinStatistics
        {
            public int Wins { get; }
            public int Losses { get; }
        }

        public void GameEndedTurn(GameBase game)
        {
        }

        public void GameEnded(GameBase game, Guid winnersIdentifiers, Guid[] loosersIdentifier)
        {
        }

        public Guid GetBestChoice(Guid player, IEnumerable<Guid> possibleOpponents)
        {
            return possibleOpponents.First();
        }

        public IWinStatistics GetWinStatistics(Guid player, Guid against)
        {
            return new WinStats();
        }

        public string GetMotd()
        {
            return "Hewlo :)";
        }
    }
}