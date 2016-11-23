using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Evaders.Server
{
    using Core.Game;

    public interface IServerSupervisor
    {
        void GameEndedTurn(GameBase game);
        void GameEnded(GameBase game, Guid winnersIdentifiers, Guid[] loosersIdentifier);
        IWinStatistics GetWinStatistics(Guid player, Guid against);
        Guid GetBestChoice(Guid player, IEnumerable<Guid> possibleOpponents);
        string GetMotd();
    }
}
