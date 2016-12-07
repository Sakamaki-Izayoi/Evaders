namespace Evaders.ServerRunner.Windows
{
    using System;
    using Server.Integration;

    internal class DefaultProviderFactory<T> : IProviderFactory<T>
    {
        private readonly Func<string, T> _factory;

        public DefaultProviderFactory(Func<string, T> factory)
        {
            _factory = factory;
        }

        public T Create(string id)
        {
            return _factory(id);
        }

        public void AddProvider(IProvider<T> provider)
        {
            throw new NotImplementedException();
        }
    }
}