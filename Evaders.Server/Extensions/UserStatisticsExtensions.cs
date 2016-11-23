using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Evaders.Server.Extensions
{
    internal static class UserStatisticsExtensions
    {
        public static int GetTotalGames(this IWinStatistics statistics) => statistics.Losses + statistics.Wins;

    }
}
