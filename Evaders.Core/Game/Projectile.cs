namespace Evaders.Core.Game
{
    using System;
    using Newtonsoft.Json;
    using Utility;

    public class Projectile
    {
        [JsonProperty]
        public Vector2 Position { get; internal set; }

        public readonly int Damage;

        [JsonProperty]
        public readonly Vector2 Direction;

        [JsonProperty]
        public readonly long EntityIdentifier;

        [JsonProperty]
        public readonly int HitboxSize;

        [JsonProperty]
        public readonly int LifeEndTurn;

        [JsonProperty]
        public readonly long PlayerIdentifier;

        [JsonProperty]
        public readonly long ProjectileIdentifier;

        [JsonProperty]
        public readonly double ProjectileSpeedSec;

        public Vector2 DespawnPosition => Position + Direction * ProjectileSpeedSec * Game.TimePerFrameSec * (LifeEndTurn - Game.Turn);
        public Vector2 PositionNextTurn => Position + Direction * ProjectileSpeedSec * Game.TimePerFrameSec;
        public double MovingDistancePerTurn => ProjectileSpeedSec * Game.TimePerFrameSec;

        internal GameBase Game;

        internal Projectile(Vector2 direction, EntityBase entity, GameBase game, long projectileIdentifier, int lifeEndTurn)
        {
            if (!direction.IsUnitVector)
            {
                direction = direction.Unit; //Bug: .Unit is slightly inaccurate, causing the above check to fail :S this is a workaround
                //throw new ArgumentException("Not a direction (unit vector)", nameof(direction));
            }

            Position = entity.Position + direction * (entity.CharData.HitboxSize + entity.CharData.ProjectileHitboxSize);
            Direction = direction;
            HitboxSize = entity.CharData.ProjectileHitboxSize;
            Damage = entity.CharData.ProjectileDamage;
            PlayerIdentifier = entity.PlayerIdentifier;
            EntityIdentifier = entity.EntityIdentifier;
            ProjectileIdentifier = projectileIdentifier;
            LifeEndTurn = lifeEndTurn;
            ProjectileSpeedSec = entity.CharData.ProjectileSpeedSec;

            Game = game;
        }

        [JsonConstructor]
        private Projectile(int damage, Vector2 direction, long entityIdentifier, int hitboxSize, int lifeEndTurn, long playerIdentifier, long projectileIdentifier, double projectileSpeedSec, Vector2 position)
        {
            Damage = damage;
            Direction = direction;
            EntityIdentifier = entityIdentifier;
            HitboxSize = hitboxSize;
            LifeEndTurn = lifeEndTurn;
            PlayerIdentifier = playerIdentifier;
            ProjectileIdentifier = projectileIdentifier;
            ProjectileSpeedSec = projectileSpeedSec;
            Position = position;
        }

        //public bool WillHit(EntityBase entity, Vector2 assumedEntityWaypoint)
        //{
        //    for (var i = 1u; i < 100; i++)
        //    {
        //        if (WillHitIn(entity, i, assumedEntityWaypoint))
        //            return true;
        //    }
        //    return false;

        //    var startPath = entity.Position;

        //    if (startPath.Distance(assumedEntityWaypoint, true) <= 1d) // entity standing still
        //    {
        //        return WillHitIn(entity, (uint)DistanceToFlooredTurnCount(Position.Distance(entity.Position)), assumedEntityWaypoint);
        //    }

        //    startPath = startPath.ExtendedAway(assumedEntityWaypoint, HitboxSize + entity.HitboxSize);
        //    var endPath = assumedEntityWaypoint.ExtendedAway(startPath, HitboxSize + entity.HitboxSize);

        //    var normalDirection = (endPath - startPath).Unit;
        //    var normal1 = new Vector2(normalDirection.Y, -normalDirection.X);
        //    var normal2 = new Vector2(-normalDirection.Y, normalDirection.X);

        //    var startPathH1 = startPath + normal1 * entity.HitboxSize;
        //    var startPathH2 = startPath + normal2 * entity.HitboxSize;

        //    var endPathH1 = endPath + normal1 * entity.HitboxSize;
        //    var endPathH2 = endPath + normal2 * entity.HitboxSize;

        //    var projNormalDirection = Direction;
        //    var projNormal1 = new Vector2(projNormalDirection.Y, -projNormalDirection.X);
        //    var projNormal2 = new Vector2(-projNormalDirection.Y, projNormalDirection.X);

        //    var projStartPathH1 = Position + projNormal1 * HitboxSize;
        //    var projStartPathH2 = Position + projNormal2 * HitboxSize;

        //    var projEndPathH1 = DespawnPosition + projNormal1 * HitboxSize;
        //    var projEndPathH2 = DespawnPosition + projNormal2 * HitboxSize;


        //    Vector2 intersVec;

        //    if (Geometry.LinesIntersect(startPathH1, endPathH1, projEndPathH1, projEndPathH1, out intersVec))
        //    {
        //        return WillHitIn(entity, DistanceToFlooredTurnCount(startPathH1.Distance(intersVec)), assumedEntityWaypoint);
        //    }

        //    if (Geometry.LinesIntersect(startPathH1, endPathH1, projEndPathH2, projEndPathH2, out intersVec))
        //    {
        //        return WillHitIn(entity, DistanceToFlooredTurnCount(startPathH1.Distance(intersVec)), assumedEntityWaypoint);
        //    }

        //    if (Geometry.LinesIntersect(startPathH2, endPathH2, projEndPathH1, projEndPathH1, out intersVec))
        //    {
        //        return WillHitIn(entity, DistanceToFlooredTurnCount(startPathH2.Distance(intersVec)), assumedEntityWaypoint);
        //    }

        //    if (Geometry.LinesIntersect(startPathH2, endPathH2, projEndPathH2, projEndPathH2, out intersVec))
        //    {
        //        return WillHitIn(entity, DistanceToFlooredTurnCount(startPathH2.Distance(intersVec)), assumedEntityWaypoint);
        //    }
        //    return false;

        //    //Vector2 intersVec;

        //    //if (!Geometry.LinesIntersect(startPath, endPath, Position, DespawnPosition, out intersVec))
        //    //    return false;

        //    // At this point, their path intersects somewhere - but this is only dangerous if they are there at the same time
        //    // We also have to consider that this intersection could be "between frames" which would make it invalid

        //    //var dangerousFrame = (int)(Position.Distance(intersVec) / MovingDistancePerTurn);
        //    //var turnDelta = dangerousFrame;// - Game.Turn;
        //    //for (var i = turnDelta; i < turnDelta + 2; i++) // need to check 2 frames: the frame before and after the intersection (the chance that the intersection is exactly on the frame is highly unlikely)
        //    //{
        //    //    var dangFrameEntity = entity.GetPositionIn((uint)turnDelta, assumedEntityWaypoint);
        //    //    var dangFrameProj = GetPositionIn((uint)turnDelta);
        //    //    if (dangFrameProj.Distance(dangFrameEntity, true) <= (HitboxSize + entity.HitboxSize) * (HitboxSize + entity.HitboxSize))
        //    //        return true;
        //    //}
        //    //return false;
        //}

        //public bool WillHit(EntityBase entity)
        //{
        //    return WillHit(entity, entity.MovingTo);
        //}

        /// <summary>
        /// Returns how many turns are needed to fly the given distance. If e.g. 5.5 turns are needed, this will return 5
        /// </summary>
        /// <param name="distance"></param>
        /// <returns></returns>
        public uint DistanceToFlooredTurnCount(double distance)
        {
            if (distance <= 0)
                return 0;
            return (uint)Math.Floor(distance / MovingDistancePerTurn);
        }

        /// <summary>
        /// Returns if this projectile will overlap with the given entity in the given count of turns. Assumes that the entity is moving to the given waypoint.
        /// </summary>
        public bool WillHitIn(EntityBase entity, uint turnCount, Vector2 assumedEntityWaypoint)
        {
            for (var i = turnCount; i < turnCount + 2; i++) // need to check 2 frames: the frame before and after the intersection (the chance that the intersection is exactly on the frame is highly unlikely)
            {
                var dangFrameEntity = entity.GetPositionIn(i, assumedEntityWaypoint);
                var dangFrameProj = GetPositionIn(i);
                if (dangFrameProj.Distance(dangFrameEntity, true) <= (HitboxSize + entity.HitboxSize) * (HitboxSize + entity.HitboxSize))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Returns if this projectile will overlap with the given entity in the given count of turns.
        /// </summary>
        public bool WillHitIn(EntityBase entity, uint turnCount)
        {
            return WillHitIn(entity, turnCount, entity.MovingTo);
        }

        public Vector2 GetPositionIn(uint turns)
        {
            return Position.Extended(DespawnPosition, MovingDistancePerTurn * turns);
        }

        internal void Update()
        {
            if (Game.Turn >= LifeEndTurn)
            {
                Game.HandleDeath(this);
                return;
            }

            Position = PositionNextTurn;
            foreach (var entity in Game.ValidEntities)
                if (entity.PlayerIdentifier != PlayerIdentifier && entity.Position.Distance(Position, true) <= (HitboxSize + entity.CharData.HitboxSize) * (HitboxSize + entity.CharData.HitboxSize))
                {
                    Game.HandleDeath(this);
                    entity.InflictDamage(Damage);
                    break;
                }
        }
    }
}