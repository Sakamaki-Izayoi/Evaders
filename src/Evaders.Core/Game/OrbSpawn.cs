using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Evaders.Core.Game
{
    using Newtonsoft.Json;
    using Utility;

    public abstract class OrbSpawn
    {
        public abstract int HitboxSize { get; }

        [JsonProperty]
        public abstract int LastCollectedTurn { get; protected set; }

        public int NextSpawnTurn => LastCollectedTurn + (int)Math.Ceiling(Game.Settings.HealorbRespawnSec / Game.TimePerFrameSec);
        public bool IsUp => NextSpawnTurn <= Game.Turn;

        [JsonProperty]
        public readonly Vector2 Position;

        internal GameBase Game;

        protected OrbSpawn(GameBase game, Vector2 position) : this(position)
        {
            Game = game;
        }

        protected OrbSpawn(Vector2 position)
        {
            Position = position;
        }

        public Vector2 GetNearestPickupPosition(Entity collector) => Position.Extended(collector.Position, HitboxSize + collector.HitboxSize);

        public void Update()
        {
            if (!IsUp)
                return;

            var pickedUp = false; // If multiple entities pick it up in the same frame, each of them will get the heal
            foreach (var entity in Game.Entities)
            {
                if (entity.Position.Distance(Position, true) <= (HitboxSize + entity.HitboxSize) * (HitboxSize + entity.HitboxSize))
                {
                    OnPickedUp(entity);
                    pickedUp = true;
                }
            }
            if (pickedUp)
                LastCollectedTurn = Game.Turn;
        }

        protected abstract void OnPickedUp(EntityBase collector);
    }
}
