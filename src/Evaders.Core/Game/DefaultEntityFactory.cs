namespace Evaders.Core.Game
{
    using Utility;

    public class DefaultEntityFactory : IEntityFactory<EntityBase>
    {
        public EntityBase Create(CharacterData charData, Vector2 position, long playerIdentifier, long entityIdentifier, GameBase game)
        {
            return new EntityBase(charData, position, playerIdentifier, entityIdentifier, game);
        }
    }
}