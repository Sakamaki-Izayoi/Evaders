namespace Evaders.Server.Integration
{
    using System;
    using System.Collections.Generic;
    using Core.Game;

    public interface IServerSupervisor
    {
        void GameEndedTurn(ServerGame game, List<Tuple<EntityBase, ServerGame.ChangeKind>> changedEntities, List<Tuple<Projectile, ServerGame.ChangeKind>> changedProjectiles, List<OrbSpawn> changedOrbSpawns, List<GameAction> executedGameActions);
        void GameEnded(ServerGame game, Guid winnersIdentifiers, Guid[] loosersIdentifier);
        Guid GetBestChoice(Guid player, IEnumerable<Guid> possibleOpponents);
    }
}