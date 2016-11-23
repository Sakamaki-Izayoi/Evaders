namespace Evaders.Core.Game
{
    using Newtonsoft.Json;

    public class GameSettings
    {
        public virtual bool IsValid => ArenaRadius > 0 && FramesPerSecond > 0 && MaxFrameTimeSec > 0 && DefaultCharacterData.IsValid;

        [JsonProperty] public readonly float ArenaRadius;

        [JsonProperty] public readonly CharacterData DefaultCharacterData;

        [JsonProperty] public readonly int FramesPerSecond;

        [JsonProperty] public readonly float MaxFrameTimeSec;

        [JsonProperty] public readonly float ProjectileLifeTimeSec;

        [JsonConstructor]
        public GameSettings(float arenaRadius, int framesPerSecond, float maxFrameTimeSec, float projectileLifeTimeSec, CharacterData defaultCharacterData)
        {
            DefaultCharacterData = defaultCharacterData;
            ArenaRadius = arenaRadius;
            FramesPerSecond = framesPerSecond;
            MaxFrameTimeSec = maxFrameTimeSec;
            ProjectileLifeTimeSec = projectileLifeTimeSec;
        }
    }
}