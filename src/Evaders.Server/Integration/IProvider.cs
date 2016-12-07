namespace Evaders.Server.Integration
{
    using JetBrains.Annotations;

    /// <summary>
    ///     Represents a type that can create instances of <see cref="TCreationType" />.
    /// </summary>
    public interface IProvider<out TCreationType>
    {
        /// <summary>
        ///     Represents an id which can uniquely identify a provider.
        /// </summary>
        [NotNull]
        string Id { get; }

        /// <summary>
        ///     Creates a new <see cref="TCreationType" /> instance.
        /// </summary>
        /// <returns>The <see cref="TCreationType" />.</returns>
        [NotNull]
        TCreationType Create();
    }
}