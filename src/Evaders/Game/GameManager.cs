namespace Evaders.Game
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Threading;
    using Core.Game;
    using JetBrains.Annotations;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Server;
    using Server.Integration;
    using Services;
    using Supervisors;

    [UsedImplicitly]
    public class GameManager : IGameManager
    {
        private readonly ILogger<GameManager> _logger;


        /* Server related */
        private EvadersServer _server;
        private Timer _tickTimer;

        /* Server mechanics */
        private IProviderFactory<IMatchmaking> _matchmakingProvider;
        private IProviderFactory<IServerSupervisor> _serverSupervisiorProvider;

        /* Game and Server settings */
        private IProviderFactory<GameSettings> _gameSettingsProvider;

        /* Current server settings */
        private ServerSettings _serverSettings;


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

            /*
             * ADD PROVIDERS
             */
            _matchmakingProvider.Provider("default", () => new Matchmaking(services.GetService<ILogger<Matchmaking>>()));
            _serverSupervisiorProvider.Provider("default", () => new UnrankedServerSupervisor(services.GetService<ILogger<UnrankedServerSupervisor>>(), false));

            _gameSettingsProvider.Provider("default", () => GameSettings.Default); // bug read from gamesettings.json

            /*
             * CONFIGURE serversettings.json
             */
            {
                var configurationBuilder = new ConfigurationBuilder();
                configurationBuilder.SetBasePath(hostingEnvironment.ContentRootPath);
                configurationBuilder.AddJsonFile("serversettings.json", false, true);

                var root = configurationBuilder.Build();
                OnServerSettingsReload(root);
            }

            /*
             * CONFIGURE gamesettings.json 
             */
            {
                var configurationBuilder = new ConfigurationBuilder();
                configurationBuilder.SetBasePath(hostingEnvironment.ContentRootPath);
                configurationBuilder.AddJsonFile("gamesettings.json", false, true);

                var root = configurationBuilder.Build();
                OnGameSettingsReload(root);
            }

            /*
             * START the game server
             */
            _server = new EvadersServer(_serverSupervisiorProvider, _gameSettingsProvider, _matchmakingProvider, services.GetRequiredService<ILogger<EvadersServer>>(), _serverSettings);
            _server.Start();
            _tickTimer = new Timer(e => ((EvadersServer) e).Update(), _server, 0, 32);
        }


        private static ServerSettings LoadServerSettings(IConfiguration root, ILogger logger)
        {
            var settings = new ServerSettings();

            // game specific settings
            settings.ApplySettingArray<string>("game:modes", root, (s, e) => s.GameModes = e);

            // general settings
            settings.ApplySetting("general:motd", root, logger, (s, e) => s.Motd = e);
            settings.ApplySettingCustom("general:maxUsernameLength", root, logger, (s, e) =>
            {
                int length;
                if (!int.TryParse(e, out length) || length < 1)
                    return false;
                s.MaxUsernameLength = length;
                return true;
            });

            // network settings
            settings.ApplySettingCustom("networking:ip", root, logger, (s, e) =>
            {
                IPAddress address;
                if (!IPAddress.TryParse(e, out address))
                    return false;
                s.IP = address;
                return true;
            });
            settings.ApplySettingCustom("networking:port", root, logger, (s, e) =>
            {
                ushort port;
                if (!ushort.TryParse(e, out port))
                    return false;
                s.Port = port;
                return true;
            });

            // queue settings
            settings.ApplySettingCustom("queue:maxTime", root, logger, (s, e) =>
            {
                float maxTime;
                if (!float.TryParse(e, out maxTime) || !(maxTime > 0f))
                    return false;
                s.MaxTimeInQueueSec = maxTime;
                return true;
            });

            return settings;
        }

        private static Dictionary<string, GameSettings> LoadGameSettings(IConfiguration root, ILogger logger)
        {
            // todo load game settings from config root
            return null;
        }


        private static void RegisterReload([NotNull] IConfigurationRoot root, Action<IConfigurationRoot> target)
        {
            if (root == null)
                throw new ArgumentNullException(nameof(root));
            root.GetReloadToken().RegisterChangeCallback(e => target((IConfigurationRoot) e), root);
        }


        private void OnServerSettingsReload(IConfigurationRoot root)
        {
            // register again
            RegisterReload(root, OnServerSettingsReload);

            // load the settings
            _serverSettings = LoadServerSettings(root, _logger);
            // todo set server settings

            // log that the setting have been reloaded
            _logger.LogInformation("Server settings reloaded.");
        }

        private void OnGameSettingsReload(IConfigurationRoot root)
        {
            // register again
            RegisterReload(root, OnGameSettingsReload);

            // load the settings
            var settings = LoadGameSettings(root, _logger);
            // todo set game settings

            // log that the settings have been reloaded
            _logger.LogInformation("Game settings reloaded.");
        }
    }
}