namespace Evaders.Server.Integration
{
    /// <inheritdoc />
    public interface IProviderFactory<TCreationType> : IFactory<TCreationType, IProvider<TCreationType>>
    {
    }
}