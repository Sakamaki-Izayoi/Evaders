namespace Evaders.Core.Game
{
    using System.Collections.Generic;

    public abstract class DefaultGame<TUser> : Game<TUser, EntityBase> where TUser : IUser
    {
        public DefaultGame(IEnumerable<TUser> users, GameSettings settings) : base(users, settings, new DefaultEntityFactory())
        {
        }
    }
}