namespace Evaders.Services.Providers
{
    using System;
    using JetBrains.Annotations;
    using Server.Integration;

    [UsedImplicitly]
    public class DefaultProvider<TCreationType> : IProvider<TCreationType>
    {
        private readonly Func<TCreationType> _factory;


        /// <inheritdoc />
        public string Id { get; }


        public DefaultProvider([NotNull] string id, [NotNull] Func<TCreationType> factory)
        {
            if (id == null) throw new ArgumentNullException(nameof(id));
            if (factory == null) throw new ArgumentNullException(nameof(factory));

            _factory = factory;
            Id = id;
        }


        /// <inheritdoc />
        public TCreationType Create() => _factory();
    }
}