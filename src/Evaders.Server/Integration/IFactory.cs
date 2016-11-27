namespace Evaders.Server.Integration
{
    using JetBrains.Annotations;

    /// <summary>
    ///   Represents a type used to configure and create instances of <see cref="TCreationType" />
    ///   from the registered <see cref="TProviderType" />s.
    /// </summary>
    public interface IFactory<out TCreationType, in TProviderType> where TProviderType : IProvider<TCreationType>
    {
        /// <summary>
        ///   Creates a new <see cref="TCreationType" /> instance.
        /// </summary>
        /// <param name="id">The <see cref="TProviderType"/> id which is used for creating the <see cref="TCreationType" /></param>
        /// <returns>The <see cref="TCreationType" />.</returns>
        [NotNull]
        TCreationType Create([NotNull] string id);

        /// <summary>
        ///   Adds an <see cref="TProviderType" /> to the supervisor system.
        /// </summary>
        /// <param name="provider">The <see cref="TProviderType"/>.</param>
        void AddProvider([NotNull] TProviderType provider);
    }
}