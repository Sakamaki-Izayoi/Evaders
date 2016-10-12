namespace Evaders.Core.Game
{
    public class GameSettings
    {
        public bool IsValid => ArenaRadius > 0 && FramesPerSecond > 0 && MaxFrameTimeSec > 0 && DefaultCharacterData.IsValid;
        public readonly float ArenaRadius;
        public readonly CharacterData DefaultCharacterData;
        public readonly int FramesPerSecond;
        public readonly float MaxFrameTimeSec;

        public GameSettings(float arenaRadius, int framesPerSecond, float maxFrameTimeSec, CharacterData defaultCharacterData)
        {
            ArenaRadius = arenaRadius;
            FramesPerSecond = framesPerSecond;
            MaxFrameTimeSec = maxFrameTimeSec;
            DefaultCharacterData = defaultCharacterData;
        }
    }
}