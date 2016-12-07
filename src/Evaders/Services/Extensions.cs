namespace Evaders.Services
{
    using System;
    using Microsoft.Extensions.DependencyInjection;
    using Server.Integration;

    public static class Extensions
    {
        public static IProviderFactory<T> GetRequiredProvider<T>(this IServiceProvider services)
        {
            return services.GetRequiredService<IProviderFactory<T>>();
        }
    }
}