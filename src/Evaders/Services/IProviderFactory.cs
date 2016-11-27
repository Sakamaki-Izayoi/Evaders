namespace Evaders.Services
{
    /// <inheritdoc />
    public interface IProviderFactory<TCreationType> : IFactory<TCreationType, IProvider<TCreationType>>
    {
    }
}