namespace Evaders.Core.Tests
{
    using Game;

    internal class DummyUser : IUser
    {
        public bool Connected { get; }
        public long Identifier { get; }

        public DummyUser(bool connected, long identifier)
        {
            Connected = connected;
            Identifier = identifier;
        }
    }
}