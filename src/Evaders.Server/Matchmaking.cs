namespace Evaders.Server
{
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using Extensions;
    using Integration;
    using Microsoft.Extensions.Logging;

    public class Matchmaking : IMatchmaking
    {
        public IServerSupervisor Supervisor { get; set; }
        private readonly List<IServerUser> _inQueue = new List<IServerUser>();
        private readonly Dictionary<IServerUser, double> _joinedQueueTime = new Dictionary<IServerUser, double>();
        private readonly ILogger _logger;
        private readonly Stopwatch _time = Stopwatch.StartNew();
        private string _gameMode;
        private double _lastQueuerTime;
        private double _maxTimeInQueue;
        private IMatchmakingServer _server;

        public Matchmaking(ILogger logger)
        {
            _logger = logger;
        }

        public void Update()
        {
            if (_inQueue.DistinctBy(item => item.Login).Count() < 2)
                return;

            var time = _time.Elapsed.TotalSeconds;

            for (var i = 0; i < _inQueue.Count; i++)
                if (time - _joinedQueueTime[_inQueue[i]] > _maxTimeInQueue)
                {
                    var hoomanBots = _inQueue.Where(usr => !usr.IsPassiveBot && (usr != _inQueue[i])).ToArray();
                    if (hoomanBots.Any())
                    {
                        IServerUser bestHoomanBot;
                        if (hoomanBots.Length == 1)
                            bestHoomanBot = hoomanBots[0];
                        else
                        {
                            var bestMatch = Supervisor.GetBestChoice(_inQueue[i].Login, hoomanBots.Select(item => item.Login));
                            bestHoomanBot = hoomanBots.FirstOrDefault(bot => bot.Login == bestMatch);
                        }
                        if (bestHoomanBot == null)
                        {
                            _logger.LogError($"{GetType().Name}: {Supervisor.GetType()} gave me an invalid GUID as best choice!");
                            continue;
                        }

                        _logger.LogDebug($"{_inQueue[i]} found a match: {bestHoomanBot})");
                        OnFoundMatchup(bestHoomanBot, _inQueue[i]);
                    }
                    else
                    {
                        var bestMatch = Supervisor.GetBestChoice(_inQueue[i].Login, _inQueue.Where(item => item != _inQueue[i]).ToArray().Select(item => item.Login));
                        var bestBotBot = hoomanBots.FirstOrDefault(bot => bot.Login == bestMatch);

                        if (bestBotBot == null)
                        {
                            _logger.LogError($"{GetType().Name}: {Supervisor.GetType()} gave me an invalid GUID as best choice!");
                            continue;
                        }

                        _logger.LogDebug($"{_inQueue[i]} exceeded max queue time, matching with bot: {bestBotBot})");
                        OnFoundMatchup(bestBotBot, _inQueue[i]);
                    }
                }
        }

        public void Configure(IMatchmakingServer server, IServerSupervisor supervisor, string gameMode, double maxTimeInQueueSec)
        {
            Supervisor = supervisor;
            _server = server;
            _gameMode = gameMode;
            _maxTimeInQueue = maxTimeInQueueSec;

            _logger.LogDebug("Configured matchmaking: " + gameMode);
        }

        public bool HasUser(IServerUser user)
        {
            return _inQueue.Any(usr => usr == user);
        }

        public void EnterQueue(IServerUser user)
        {
            var time = _time.Elapsed.TotalSeconds;
            if ((time - _lastQueuerTime > _maxTimeInQueue) && _inQueue.All(item => item.IsPassiveBot) && _inQueue.Any() && !user.IsPassiveBot)
            {
                AddUser(user);

                var bestMatch = Supervisor.GetBestChoice(user.Login, _inQueue.Where(item => item != user).ToArray().Select(item => item.Login));
                var bestBotBot = _inQueue.FirstOrDefault(bot => bot.Login == bestMatch);

                if (bestBotBot == null)
                    _logger.LogError($"{GetType().Name}: {Supervisor.GetType()} gave me an invalid GUID as best choice!");
                else
                {
                    _logger.LogDebug($"Found a match (Queue very empty for a longer time, matching with bot: {bestBotBot})");
                    OnFoundMatchup(user, bestBotBot);
                    return;
                }
            }


            if (!user.IsPassiveBot)
                _lastQueuerTime = time;

            AddUser(user);
        }

        public void LeaveQueue(IServerUser user)
        {
            _inQueue.Remove(user);
            _logger.LogDebug($"{user} left matchmaking");
        }

        private void OnFoundMatchup(params IServerUser[] users)
        {
            _server.CreateGame(_gameMode, this, users);
        }


        private void AddUser(IServerUser user)
        {
            _inQueue.Add(user);
            _joinedQueueTime[user] = _time.Elapsed.TotalSeconds;
            _logger.LogDebug($"{user} entered matchmaking");
        }
    }
}