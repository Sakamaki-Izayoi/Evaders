using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Evaders.Server
{
    using Core.Game;

    public class GameChangeTracker
    {
        public enum ChangeKind { Spawn, Death, Health }

        public readonly List<Tuple<EntityBase, ChangeKind>> ChangedEntities = new List<Tuple<EntityBase, ChangeKind>>();
        public readonly List<Tuple<Projectile, ChangeKind>> ChangedProjectiles = new List<Tuple<Projectile, ChangeKind>>();
        public readonly List<OrbSpawn> ChangedOrbSpawners = new List<OrbSpawn>();
        public readonly List<GameAction> ExecutedGameActions = new List<GameAction>();
    }
}
