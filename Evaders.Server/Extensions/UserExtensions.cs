using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Evaders.Server.Extensions
{
    internal static class UserExtensions
    {
        public static bool HasEverPlayedAgainst(this IServerUser me, IServerUser other, IServerSupervisor supervisor)
        {
            return supervisor.GetWinStatistics(me.Login, other.Login).GetTotalGames() > 0;
        }
    }
}
