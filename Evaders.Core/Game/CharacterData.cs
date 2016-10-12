namespace Evaders.Core.Game
{
    public class CharacterData
    {
        public bool IsValid => MaxHealth > 0 && ProjectileSpeedSec > 0 && ProjectileDamage > 0 && ProjectileHitboxSize > 0 && ReloadDelaySec > 0 && HitboxSize > 0 && SpeedSec > 0;
        public readonly int HitboxSize;
        public readonly float MaxHealth;
        public readonly float ProjectileDamage;
        public readonly float ProjectileHitboxSize;
        public readonly float ProjectileSpeedSec;
        public readonly float ReloadDelaySec;
        public readonly float SpeedSec;

        public CharacterData(float maxHealth, float projectileDamage, float projectileHitboxSize, float reloadDelaySec, int hitboxSize, float projectileSpeedSec, float speedSec)
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