namespace Evaders.Game
{
    using System;

    /// <summary>
    /// Represents a class which manages game files, configurations and more.
    /// </summary>
    public interface IGameManager
    {
        /// <summary>
        ///   Configures the game manager and all related services.
        /// </summary>
        /// <param name="services">The service provider from which the related services will be gathered.</param>
        void Configure(IServiceProvider services);
    }
}