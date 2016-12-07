using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Evaders.Core.Game
{
    using Newtonsoft.Json;
    using Utility;

    public class CloneOrbSpawn : OrbSpawn
    {
        public override int HitboxSize => Game.Settings.CloneorbHitboxSize;
        public override int LastCollectedTurn { get; protected set; }

        public CloneOrbSpawn(GameBase game, Vector2 position) : base(game, position)
        {
        }

        [JsonConstructor]
        public CloneOrbSpawn(Vector2 position) : base(position)
        {
        }

        protected override void OnPickedUp(EntityBase collector)
        {
            collector.SpawnClone();
        }
    }
}
