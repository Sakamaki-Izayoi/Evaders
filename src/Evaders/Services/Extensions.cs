namespace Evaders.Services
{
    using System;
    using System.Linq;
    using JetBrains.Annotations;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Providers;
    using Server;
    using Server.Integration;

    public static class Extensions
    {
        public static void Provider<T>(this IProviderFactory<T> providerFactory, string id, Func<T> factory)
        {
            providerFactory.AddProvider(new DefaultProvider<T>(id, factory));
        }

        public static IProviderFactory<T> GetRequiredProvider<T>(this IServiceProvider services)
        {
            return services.GetRequiredService<IProviderFactory<T>>();
        }


        public static void ApplySetting([NotNull] this ServerSettings value, [NotNull] string key, [NotNull] IConfiguration configuration, ILogger logger, [NotNull] Action<ServerSettings, string> action)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            var data = configuration.GetValue<string>(key);
            if (data != null)
            {
                action(value, data);
            }
            else
            {
                logger.LogWarning($"Configuration value not loaded: '{key}'");
            }
        }

        public static void ApplySettingCustom([NotNull] this ServerSettings value, [NotNull] string key, [NotNull] IConfiguration configuration, ILogger logger, [NotNull] Func<ServerSettings, string, bool> action)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            var data = configuration.GetValue<string>(key);
            if (data == null || !action(value, data))
            {
                logger.LogWarning($"Configuration value not loaded: '{key}'");
            }
        }

        public static void ApplySettingArray<TData>([NotNull] this ServerSettings value, [NotNull] string key, [NotNull] IConfiguration configuration, [NotNull] Action<ServerSettings, TData[]> action)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            var section = configuration.GetSection(key);
            action(value, section.GetChildren().Select(e => section.GetValue<TData>(e.Key)).ToArray());
        }
    }
}