namespace Evaders.Core.Game
{
    public interface IUser
    {
        bool Connected { get; }
        long Identifier { get; }
    }
}