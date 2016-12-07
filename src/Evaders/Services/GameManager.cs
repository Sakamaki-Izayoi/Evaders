namespace Evaders.Services
{
    using System;
    using System.Linq;
    using Core.Game;
    using JetBrains.Annotations;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Server;
    using Server.Integration;

    [UsedImplicitly]
    public class GameManager : IGameManager
    {
        private readonly ILogger<GameManager> _logger;


        /* Server */
        EvadersServer _server;

        /* Server mechanics */
        private IProviderFactory<IMatchmaking> _matchmakingProvider;
        private IProviderFactory<IServerSupervisor> _serverSupervisiorProvider;

        /* Game and Server settings */
        private IProviderFactory<GameSettings> _gameSettingsProvider;
        private IProviderFactory<ServerSettings> _serverSettingsProvider;


        public GameManager(ILogger<GameManager> logger)
        {
            _logger = logger;
        }

        /// <inheritdoc />
        public void Configure(IServiceProvider services)
        {
            var hostingEnvironment = services.GetRequiredService<IHostingEnvironment>();

            /*
             * SETUP SERVICES
             */
            _matchmakingProvider = services.GetRequiredProvider<IMatchmaking>();
            _serverSupervisiorProvider = services.GetRequiredProvider<IServerSupervisor>();

            _gameSettingsProvider = services.GetRequiredProvider<GameSettings>();
            _serverSettingsProvider = services.GetRequiredProvider<ServerSettings>();

            /*
             * CONFIGURE serversettings.json
             */

            /*
             * CONFIGURE gamesettings.json 
             */
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.SetBasePath(hostingEnvironment.ContentRootPath);
            configurationBuilder.AddJsonFile("gamesettings.json", false, true);

            var configurationRoot = configurationBuilder.Build();
            RegisterReload(configurationRoot);
        }

        private void RegisterReload(IConfigurationRoot root)
        {
            var reloadToken = root.GetReloadToken();
            if (reloadToken.HasChanged)
            {
                _logger.LogWarning("nope");
                throw new NotSupportedException();
            }
            reloadToken.RegisterChangeCallback(OnGamesettingsReload, root);
            if (reloadToken != root.GetReloadToken())
            {
                _logger.LogError("noooooo");
            }
        }

        private void OnGamesettingsReload(object data)
        {
            var root = (IConfigurationRoot) data;
            RegisterReload(root);

            _logger.LogWarning("configuration reloaded!");
        }
    }
}