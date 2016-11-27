namespace Evaders.Services.Factories
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
    using JetBrains.Annotations;

    /// <summary>
    ///   Simple thread-safe and generic implementation of <see cref="IFactory{TCreationType,TProviderType}" />.
    /// </summary>
    /// <returns>
    ///   Optimized for repeating <see cref="AddProvider" /> calls and aftwards many <see cref="Create" /> calls and no
    ///   <see cref="AddProvider" /> calls.
    /// </returns>
    [UsedImplicitly]
    public class DefaultFactory<TCreationType> : IProviderFactory<TCreationType>
    {
        private bool _updated;
        private readonly Queue<IProvider<TCreationType>> _newProviders = new Queue<IProvider<TCreationType>>(16);
        private Dictionary<string, IProvider<TCreationType>> _providers = new Dictionary<string, IProvider<TCreationType>>();
        private readonly object _lock = new object();


        /// <inheritdoc />
        public void AddProvider(IProvider<TCreationType> provider)
        {
            if (provider == null)
                throw new ArgumentNullException(nameof(provider));

            lock (_lock)
            {
                var key = provider.Id;
                if (_providers.ContainsKey(key) || _newProviders.Any(e => e.Id == key))
                    throw new InvalidOperationException("A provider with the specified id is already registered.");
                _newProviders.Enqueue(provider);
                _updated = true;
            }
        }

        /// <inheritdoc />
        public TCreationType Create(string id)
        {
            if (id == null)
                throw new ArgumentNullException(nameof(id));

            // update the providers if nessesary
            if (_updated)
                UpdateProviders();

            // try to fetch the target provider
            var providers = _providers;
            IProvider<TCreationType> type;
            if (!providers.TryGetValue(id, out type))
                throw new InvalidOperationException("No provider with the specified name is registered.");

            Debug.Assert(type != null);
            return type.Create();
        }


        /// <summary>
        ///   Updates the provider list in a thread-safe manner.
        /// </summary>
        private void UpdateProviders()
        {
            lock (_lock)
            {
                var providers = _providers;
                var newDict = new Dictionary<string, IProvider<TCreationType>>(providers);
                while (_newProviders.Count > 0)
                {
                    var item = _newProviders.Dequeue();
                    newDict.Add(item.Id, item);
                }
                if (Interlocked.CompareExchange(ref _providers, newDict, providers) != providers)
                    throw new InvalidOperationException("The providers changed while updating.");
                _updated = false;
            }
        }
    }
}