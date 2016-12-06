using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Evaders.Core.Game
{
    using Newtonsoft.Json;
    using Utility;

    public class HealOrbSpawn : OrbSpawn
    {
        public override int HitboxSize => Game.Settings.HealorbHitboxSize;
        public override int LastCollectedTurn { get; protected set; }
        public int HealAmount => Game.Settings.HealorbHealAmount;

        internal HealOrbSpawn(GameBase game, Vector2 position) : base(game, position) { }

        [JsonConstructor]
        private HealOrbSpawn(Vector2 position) : base(position) { }

        // ReSharper disable once ImpureMethodCallOnReadonlyValueField
        protected override void OnPickedUp(EntityBase collector)
        {
            collector.InflictDamage(-HealAmount);
        }
    }
}
