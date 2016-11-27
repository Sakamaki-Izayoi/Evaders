namespace Evaders.Services
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Factories;
    using JetBrains.Annotations;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Server;

    [UsedImplicitly]
    public class DefaultGameServer : IGameServer
    {
        /* IDisposable stuff */
        private bool _disposed;

        /* Stuff from constructor */
        private readonly ILoggerFactory _loggerFactory;
        private readonly IProviderFactory<IServerSupervisor> _serverSupervisorFactory;
        private readonly IProviderFactory<IMatchmaking> _matchmakingFactory;
        private readonly IProviderFactory<ServerConfiguration> _serverConfigurationFactory;
        private readonly GameServerSettings _settings;

        /* Task related stuff */
        private Task _gameServerLoop;
        private readonly CancellationTokenSource _cancellation;

        /* Game server related stuff */
        private EvadersServer _server;


        public DefaultGameServer(ILoggerFactory loggerFactory, IProviderFactory<IServerSupervisor> serverSupervisorFactory, IProviderFactory<IMatchmaking> matchmakingFactory, IProviderFactory<ServerConfiguration> serverConfigurationFactory, IOptions<GameServerSettings> settings)
        {
            _loggerFactory = loggerFactory;
            _serverSupervisorFactory = serverSupervisorFactory;
            _matchmakingFactory = matchmakingFactory;
            _serverConfigurationFactory = serverConfigurationFactory;
            _settings = settings.Value;
            _cancellation = new CancellationTokenSource();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _cancellation.Dispose();
            _disposed = true;
        }

        /// <inheritdoc />
        public void Start()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(DefaultGameServer));

            _server = new EvadersServer(_serverSupervisorFactory.Create(_settings.SupervisorProviderId), _matchmakingFactory.Create(_settings.MatchmakingProviderId), _loggerFactory.CreateLogger<EvadersServer>(), _serverConfigurationFactory.Create(_settings.ServerConfigurationProviderId));

            _gameServerLoop = new Task(GameLoop, _cancellation.Token, _cancellation.Token, TaskCreationOptions.LongRunning);
            _gameServerLoop.Start();
        }


        private async void GameLoop(object target)
        {
            var token = (CancellationToken)target;
            var server = _server;

            if (server == null) throw new ArgumentException();

            while (true)
            {
                token.ThrowIfCancellationRequested();
                server.Update();

                await Task.Delay(75, token);
            }
        }
    }
}