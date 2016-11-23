namespace Evaders.Server.Extensions
{
    internal static class UserStatisticsExtensions
    {
        public static int GetTotalGames(this IWinStatistics statistics) => statistics.Losses + statistics.Wins;
    }
}