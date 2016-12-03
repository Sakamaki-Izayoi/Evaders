namespace Evaders.Game.Servers
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Core.Game;
    using JetBrains.Annotations;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Server;
    using Server.Integration;
    using Services;

    [UsedImplicitly]
    public class DefaultGameServer : IGameServer
    {
        /* IDisposable stuff */
        private bool _disposed;

        /* Stuff from constructor */
        private readonly ILoggerFactory _loggerFactory;
        private readonly IProviderFactory<IServerSupervisor> _serverSupervisorFactory;
        private readonly IProviderFactory<IMatchmaking> _matchmakingFactory;
        private readonly IProviderFactory<GameSettings> _gameSettingsFactory;
        private readonly IProviderFactory<ServerSettings> _serverConfigurationFactory;
        private readonly GameServerSettings _settings;

        /* Task related stuff */
        private Task _gameServerLoop;
        private CancellationTokenSource _cancellation;

        /* Game server related stuff */
        private EvadersServer _server;

        /* If the game server is currently running */
        private bool _running = false;

        /* Logging */
        private readonly ILogger _logger;


        public DefaultGameServer(ILoggerFactory loggerFactory, IProviderFactory<IServerSupervisor> serverSupervisorFactory, IProviderFactory<IMatchmaking> matchmakingFactory, IProviderFactory<GameSettings> gameSettingsFactory, IProviderFactory<ServerSettings> serverConfigurationFactory, IOptions<GameServerSettings> settings)
        {
            _loggerFactory = loggerFactory;
            _logger = loggerFactory.CreateLogger<DefaultGameServer>();
            _serverSupervisorFactory = serverSupervisorFactory;
            _matchmakingFactory = matchmakingFactory;
            _gameSettingsFactory = gameSettingsFactory;
            _serverConfigurationFactory = serverConfigurationFactory;
            _settings = settings.Value;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _cancellation.Dispose();
            Stop();
            _disposed = true;
        }

        /// <inheritdoc />
        public void Start()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(DefaultGameServer));
            if (_running)
                throw new InvalidOperationException("The server is already running.");
            _running = true;

            _logger.LogInformation("Starting game server ...");

            _cancellation = new CancellationTokenSource();

            _server = new EvadersServer(_serverSupervisorFactory, _gameSettingsFactory, _matchmakingFactory, _loggerFactory.CreateLogger<EvadersServer>(), _serverConfigurationFactory.Create(_settings.ServerConfigurationProviderId));

            _gameServerLoop = new Task(GameLoop, _cancellation.Token, _cancellation.Token, TaskCreationOptions.LongRunning);
            _gameServerLoop.Start();
        }

        /// <inheritdoc />
        public void Stop()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(DefaultGameServer));
            if (!_running) return;
            _running = false;

            _logger.LogInformation($"Stopping game server ...");

            _cancellation.Dispose();
            _gameServerLoop.Wait();
            _gameServerLoop = null;
        }


        private async void GameLoop(object target)
        {
            var token = (CancellationToken) target;
            var server = _server;

            if (server == null)
                throw new ArgumentException();

            _logger.LogInformation("Starting game server loop ...");

            while (true)
            {
                if (token.IsCancellationRequested) break;
                server.Update();

                await Task.Delay(75, token);
            }

            _logger.LogInformation("Game server loop stopped.");
        }
    }
}