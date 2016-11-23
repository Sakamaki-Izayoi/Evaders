using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Evaders.ServerRunner.Windows
{
    using Core.Game;
    using Server;
    class EmptySuperviser : IServerSupervisor
    {
        class WinStats : IWinStatistics
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

        public IWinStatistics GetWinStatistics(Guid player, Guid against)
        {
            return new WinStats();
        }

        public Guid GetBestChoice(Guid player, IEnumerable<Guid> possibleOpponents)
        {
            return possibleOpponents.First();
        }

        public string GetMotd()
        {
            return "Hewlo :)";
        }
    }
}
