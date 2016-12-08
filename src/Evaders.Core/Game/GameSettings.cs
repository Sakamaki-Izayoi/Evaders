namespace Evaders.Core.Game
{
    using System;
    using Newtonsoft.Json;

    public class GameSettings
    {
        public static GameSettings Default => new GameSettings(1000f, 30, 0.5f, 10f, new CharacterData(100, 10, 20, 0.75f, 65, 250f, 100f, 75), 50f, 10f, 1, 20, 10d, 20, 25, 10d);
        public virtual bool IsValid => (ArenaRadius > 0) && (TurnsPerSecond > 0) && (MaxTurnTimeSec > 0) && DefaultCharacterData.IsValid && (ArenaShrinkStartTurn >= 0) && (ArenaShrinkPerSec > 0f) && (OutOfArenaDamagePerTurn > 0) && (HealorbHitboxSize >= 0) && (HealorbHealAmount > 0) && (HealorbRespawnSec > 0) && (CloneorbHitboxSize >= 0) && (CloneorbRespawnSec > 0);

        [JsonProperty]
        public int ArenaShrinkStartTurn => (int)Math.Ceiling(ArenaShrinkStartSec / (1d / TurnsPerSecond));

        [JsonProperty]
        public double ArenaRadius { get; set; }

        [JsonProperty]
        public double ArenaShrinkPerSec { get; set; }

        [JsonProperty]
        public double ArenaShrinkStartSec { get; set; }

        [JsonProperty]
        public int CloneorbHitboxSize { get; set; }

        [JsonProperty]
        public double CloneorbRespawnSec { get; set; }

        [JsonProperty]
        public CharacterData DefaultCharacterData { get; set; }

        [JsonProperty]
        public int HealorbHealAmount { get; set; }

        [JsonProperty]
        public int HealorbHitboxSize { get; set; }

        [JsonProperty]
        public double HealorbRespawnSec { get; set; }

        [JsonProperty]
        public double MaxTurnTimeSec { get; set; }

        [JsonProperty]
        public int OutOfArenaDamagePerTurn { get; set; }

        [JsonProperty]
        public double ProjectileLifeTimeSec { get; set; }

        [JsonProperty]
        public int TurnsPerSecond { get; set; }


        [JsonConstructor]
        public GameSettings(double arenaRadius, int turnsPerSecond, double maxTurnTimeSec, double projectileLifeTimeSec, CharacterData defaultCharacterData, double arenaShrinkStartSec, double arenaShrinkPerSec, int outOfArenaDamagePerTurn, int healorbHitboxSize, double healorbRespawnSec, int healorbHealAmount, int cloneorbHitboxSize, double cloneorbRespawnSec)
        {
            DefaultCharacterData = defaultCharacterData;
            ArenaShrinkStartSec = arenaShrinkStartSec;
            ArenaShrinkPerSec = arenaShrinkPerSec;
            OutOfArenaDamagePerTurn = outOfArenaDamagePerTurn;
            HealorbHitboxSize = healorbHitboxSize;
            HealorbRespawnSec = healorbRespawnSec;
            HealorbHealAmount = healorbHealAmount;
            CloneorbHitboxSize = cloneorbHitboxSize;
            CloneorbRespawnSec = cloneorbRespawnSec;
            ArenaRadius = arenaRadius;
            TurnsPerSecond = turnsPerSecond;
            MaxTurnTimeSec = maxTurnTimeSec;
            ProjectileLifeTimeSec = projectileLifeTimeSec;
        }

        public GameSettings() { }
    }
}