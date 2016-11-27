namespace Evaders.Game
{
    using Server;


    public class WinStatistic : IWinStatistics
    {
        /// <inheritdoc />
        public int Losses { get; }

        /// <inheritdoc />
        public int Wins { get; }


        public WinStatistic(int wins, int losses)
        {
            Wins = wins;
            Losses = losses;
        }
    }
}