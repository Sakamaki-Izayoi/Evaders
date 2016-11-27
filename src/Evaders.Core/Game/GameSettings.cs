namespace Evaders.Core.Game
{
    using System;
    using Newtonsoft.Json;

    public class GameSettings
    {
        public virtual bool IsValid => ArenaRadius > 0 && TurnsPerSecond > 0 && MaxTurnTimeSec > 0 && DefaultCharacterData.IsValid && ArenaShrinkStartTurn >= 0 && ArenaShrinkPerSec > 0f && OutOfArenaDamagePerTurn > 0;

        [JsonProperty]
        public int ArenaShrinkStartTurn => (int) Math.Ceiling(ArenaShrinkStartSec/(1d/TurnsPerSecond));

        [JsonProperty] public readonly double ArenaRadius;

        [JsonProperty] public readonly double ArenaShrinkPerSec;

        [JsonProperty] public readonly double ArenaShrinkStartSec;

        [JsonProperty] public readonly CharacterData DefaultCharacterData;

        [JsonProperty] public readonly double MaxTurnTimeSec;

        [JsonProperty] public readonly int OutOfArenaDamagePerTurn = 1;

        [JsonProperty] public readonly double ProjectileLifeTimeSec;

        [JsonProperty] public readonly int TurnsPerSecond;

        [JsonConstructor]
        public GameSettings(double arenaRadius, int turnsPerSecond, double maxTurnTimeSec, double projectileLifeTimeSec, CharacterData defaultCharacterData, double arenaShrinkStartSec, double arenaShrinkPerSec, int outOfArenaDamagePerTurn)
        {
            DefaultCharacterData = defaultCharacterData;
            ArenaShrinkStartSec = arenaShrinkStartSec;
            ArenaShrinkPerSec = arenaShrinkPerSec;
            OutOfArenaDamagePerTurn = outOfArenaDamagePerTurn;
            ArenaRadius = arenaRadius;
            TurnsPerSecond = turnsPerSecond;
            MaxTurnTimeSec = maxTurnTimeSec;
            ProjectileLifeTimeSec = projectileLifeTimeSec;
        }
    }
}