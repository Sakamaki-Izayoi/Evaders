using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Evaders.ServerRunner.Windows
{
    using Server.Integration;
    class DefaultProviderFactory<T> : IProviderFactory<T>
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
