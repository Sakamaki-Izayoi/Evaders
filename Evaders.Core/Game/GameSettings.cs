namespace Evaders.Core.Game
{
    public class GameSettings
    {
        public virtual bool IsValid => ArenaRadius > 0 && FramesPerSecond > 0 && MaxFrameTimeSec > 0 && DefaultCharacterData.IsValid;
        public readonly float ArenaRadius;
        public readonly CharacterData DefaultCharacterData;
        public readonly int FramesPerSecond;
        public readonly float MaxFrameTimeSec;
        public readonly float ProjectileLifeTimeSec;

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