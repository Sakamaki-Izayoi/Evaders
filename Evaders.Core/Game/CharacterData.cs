namespace Evaders.Core.Game
{
    public class CharacterData
    {
        public bool IsValid => MaxHealth > 0 && ProjectileSpeedSec > 0 && ProjectileDamage > 0 && ProjectileHitboxSize > 0 && ReloadDelaySec > 0 && HitboxSize > 0 && SpeedSec > 0;
        public readonly int HitboxSize;
        public readonly int MaxHealth;
        public readonly int ProjectileDamage;
        public readonly int ProjectileHitboxSize;
        public readonly double ProjectileSpeedSec;
        public readonly double ReloadDelaySec;
        public readonly double SpeedSec;

        public CharacterData(int maxHealth, int projectileDamage, int projectileHitboxSize, double reloadDelaySec, int hitboxSize, double projectileSpeedSec, double speedSec)
        {
            MaxHealth = maxHealth;
            ProjectileDamage = projectileDamage;
            ProjectileHitboxSize = projectileHitboxSize;
            ReloadDelaySec = reloadDelaySec;
            HitboxSize = hitboxSize;
            ProjectileSpeedSec = projectileSpeedSec;
            SpeedSec = speedSec;
        }
    }
}