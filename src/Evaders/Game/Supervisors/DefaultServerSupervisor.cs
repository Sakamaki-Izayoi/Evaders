namespace Evaders.Game.Supervisors
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Core.Game;
    using JetBrains.Annotations;
    using Microsoft.Extensions.Logging;
    using Server;
    using Server.Integration;

    [UsedImplicitly]
    public class DefaultServerSupervisor : IServerSupervisor
    {
        private readonly ILogger<DefaultServerSupervisor> _logger;

        public DefaultServerSupervisor(ILogger<DefaultServerSupervisor> logger)
        {
            _logger = logger;
        }


        /// <inheritdoc />
        public void GameEndedTurn(GameBase game)
        {
            _logger.LogInformation("Game turn ended.");
        }

        /// <inheritdoc />
        public void GameEnded(GameBase game, Guid winnersIdentifiers, Guid[] loosersIdentifier)
        {
            _logger.LogInformation("Game turn ended.");
        }

        /// <inheritdoc />
        public IWinStatistics GetWinStatistics(Guid player, Guid against)
        {
            _logger.LogInformation("Returning game statistic.");
            return new WinStatistic(0, 0); // todo fetch win statistic from database
        }

        /// <inheritdoc />
        public Guid GetBestChoice(Guid player, IEnumerable<Guid> possibleOpponents)
        {
            _logger.LogInformation("Gettting best choice.");
            return possibleOpponents.First();
        }
    }
}