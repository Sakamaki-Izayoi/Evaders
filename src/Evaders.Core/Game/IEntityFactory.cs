namespace Evaders.Core.Game
{
    using Utility;

    public interface IEntityFactory<out TEntity> where TEntity : EntityBase
    {
        TEntity Create(CharacterData charData, Vector2 position, long playerIdentifier, long entityIdentifier, GameBase game);
    }
}