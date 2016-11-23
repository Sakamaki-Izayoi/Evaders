namespace Evaders.Core.Game
{
    using System.Collections.Generic;

    public abstract class DefaultSandboxGame<TUser> : Game<TUser, ControllableEntity> where TUser : IUser
    {
        public IReadOnlyList<ControllableEntity> ValidEntitesControllable => Entities;

        protected DefaultSandboxGame(IEnumerable<TUser> users, GameSettings settings) : base(users, settings, new ControllableEntityFactory())
        {
        }
    }
}