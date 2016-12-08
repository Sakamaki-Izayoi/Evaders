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

        public void GameEndedTurn(ServerGame game, List<Tuple<EntityBase, ServerGame.ChangeKind>> changedEntities, List<Tuple<Projectile, ServerGame.ChangeKind>> changedProjectiles, List<OrbSpawn> changedOrbSpawns, List<GameAction> executedGameActions)
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