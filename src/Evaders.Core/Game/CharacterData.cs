namespace Evaders.Core.Game
{
    using Newtonsoft.Json;

    public class CharacterData
    {
        public bool IsValid => MaxHealth > 0 && ProjectileSpeedSec > 0 && ProjectileDamage > 0 && ProjectileHitboxSize > 0 && ReloadDelaySec > 0 && HitboxSize > 0 && SpeedSec > 0;

        [JsonProperty]
        public readonly int HitboxSize;

        [JsonProperty]
        public readonly int MaxHealth;

        [JsonProperty]
        public readonly int ProjectileDamage;

        [JsonProperty]
        public readonly int ProjectileHitboxSize;

        [JsonProperty]
        public readonly int ProjectileExplosionSize;

        [JsonProperty]
        public readonly double ProjectileSpeedSec;

        [JsonProperty]
        public readonly double ReloadDelaySec;

        [JsonProperty]
        public readonly double SpeedSec;

        [JsonConstructor]
        public CharacterData(int maxHealth, int projectileDamage, int projectileHitboxSize, double reloadDelaySec, int hitboxSize, double projectileSpeedSec, double speedSec, int projectileExplosionSize)
        {
            MaxHealth = maxHealth;
            ProjectileDamage = projectileDamage;
            ProjectileHitboxSize = projectileHitboxSize;
            ReloadDelaySec = reloadDelaySec;
            HitboxSize = hitboxSize;
            ProjectileSpeedSec = projectileSpeedSec;
            SpeedSec = speedSec;
            ProjectileExplosionSize = projectileExplosionSize;
        }
    }
}