namespace Evaders.Game.Supervisors
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Core.Game;
    using Microsoft.Extensions.Logging;
    using Server.Integration;

    /// <summary>
    ///   Represents an unranked matchmaking supervisor.
    /// </summary>
    public class UnrankedServerSupervisor : IServerSupervisor
    {
        private readonly ILogger _logger;


        /// <summary>
        ///   Returns the game id under which the replay or other game relevant data is stored.
        /// </summary>
        public Guid GameId { get; set; }

        /// <summary>
        ///   Returns <c>true</c> if the supervisor is recording the game.
        /// </summary>
        public bool Recording { get; }


        public UnrankedServerSupervisor(ILogger logger, bool record)
        {
            _logger = logger;
            Recording = record;

            GameId = Guid.NewGuid();
        }


        /// <inheritdoc />
        public void GameEndedTurn(GameBase game)
        {
            _logger.LogDebug("Game turn ended.");
        }

        /// <inheritdoc />
        public void GameEnded(GameBase game, Guid winnersIdentifiers, Guid[] loosersIdentifier)
        {
            _logger.LogDebug($"Game between {loosersIdentifier.Length + 1} players ended. Winner: '{winnersIdentifiers}'");
        }

        /// <inheritdoc />
        public Guid GetBestChoice(Guid player, IEnumerable<Guid> possibleOpponents)
        {
            if (player == Guid.Empty)
                throw new ArgumentOutOfRangeException(nameof(player), "The player GUID can not be empty.");
            var target = possibleOpponents.FirstOrDefault();
            if (target == Guid.Empty)
                throw new ArgumentOutOfRangeException(nameof(possibleOpponents), "The target GUID can not be empty.");
            _logger.LogDebug($"Found best choice for '{player}': {target}");
            return target;
        }
    }
}