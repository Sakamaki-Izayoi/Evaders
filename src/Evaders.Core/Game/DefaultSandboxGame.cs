namespace Evaders.Core.Game
{
    using System.Collections.Generic;

    public abstract class DefaultSandboxGame<TUser> : Game<TUser> where TUser : IUser
    {
        public IEnumerable<Entity> ValidEntitesControllable => Entities;

        protected DefaultSandboxGame(IEnumerable<TUser> users, GameSettings settings) : base(users, settings)
        {
        }
    }
}