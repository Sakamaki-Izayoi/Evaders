namespace Evaders.Core.Game
{
    using Utility;

    public class ControllableEntityFactory : IEntityFactory<ControllableEntity>
    {
        public ControllableEntity Create(CharacterData charData, Vector2 position, long playerIdentifier, long entityIdentifier, GameBase game)
        {
            return new ControllableEntity(charData, position, playerIdentifier, entityIdentifier, game);
        }
    }
}