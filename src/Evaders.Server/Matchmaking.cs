namespace Evaders.Server
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using Core.Utility;
    using Extensions;
    using Microsoft.Extensions.Logging;

    public class Matchmaking : IMatchmaking
    {
        public class MatchCreatedArgs : EventArgs
        {
            public readonly IMatchmaking Source;
            public readonly IServerUser[] Users;

            public MatchCreatedArgs(IMatchmaking source, params IServerUser[] users)
            {
                Users = users;
                Source = source;
            }
        }

        public event EventHandler<MatchCreatedArgs> OnSuggested;
        public IEnumerable<IServerUser> InQueue => _inQueue;
        private readonly List<IServerUser> _inQueue = new List<IServerUser>();
        private readonly Dictionary<IServerUser, double> _joinedQueueTime = new Dictionary<IServerUser, double>();
        private readonly ILogger _logger;
        private readonly float _maxTimeInQueue;
        private readonly IServerSupervisor _supervisor;
        private readonly Stopwatch _time = Stopwatch.StartNew();
        //private Guid _lastQueuer;
        private double _lastQueuerTime;

        public Matchmaking(float maxTimeInQueue, ILogger logger, IServerSupervisor supervisor)
        {
            _maxTimeInQueue = maxTimeInQueue;
            _logger = logger;
            _supervisor = supervisor;
        }

        public void Update()
        {
            if (_inQueue.DistinctBy(item => item.Login).Count() < 2)
                return;

            for (var i = 0; i < _inQueue.Count; i++)
                if (_joinedQueueTime[_inQueue[i]] > _maxTimeInQueue)
                {
                    var hoomanBots = _inQueue.Where(usr => !usr.IsBot && usr != _inQueue[i]).ToArray();
                    if (hoomanBots.Any())
                    {
                        IServerUser bestHoomanBot;
                        if (hoomanBots.Length == 1)
                            bestHoomanBot = hoomanBots[0];
                        else
                        {
                            var bestMatch = _supervisor.GetBestChoice(_inQueue[i].Login, hoomanBots.Select(item => item.Login));
                            bestHoomanBot = hoomanBots.FirstOrDefault(bot => bot.Login == bestMatch);
                        }
                        if (bestHoomanBot == null)
                        {
                            _logger.LogError($"{GetType().Name}: {_supervisor.GetType()} gave me an invalid GUID as best choice!");
                            continue;
                        }

                        _logger.LogDebug($"{_inQueue[i]} found a match: {bestHoomanBot})");
                        OnSuggested?.Invoke(this, new MatchCreatedArgs(this, bestHoomanBot, _inQueue[i]));
                    }
                    else
                    {
                        var bestMatch = _supervisor.GetBestChoice(_inQueue[i].Login, _inQueue.Where(item => item != _inQueue[i]).ToArray().Select(item => item.Login));
                        var bestBotBot = hoomanBots.FirstOrDefault(bot => bot.Login == bestMatch);

                        if (bestBotBot == null)
                        {
                            _logger.LogError($"{GetType().Name}: {_supervisor.GetType()} gave me an invalid GUID as best choice!");
                            continue;
                        }

                        _logger.LogDebug($"{_inQueue[i]} exceeded max queue time, matching with bot: {bestBotBot})");
                        OnSuggested?.Invoke(this, new MatchCreatedArgs(this, bestBotBot, _inQueue[i]));
                    }
                }
        }

        public int GetRegisterCount(IServerUser user)
        {
            return _inQueue.Count(usr => usr == user);
        }

        public void EnterQueue(IServerUser user)
        {
            var time = _time.Elapsed.TotalSeconds;
            if (time - _lastQueuerTime > _maxTimeInQueue && _inQueue.All(item => item.IsBot) && _inQueue.Any() && !user.IsBot)
            {
                AddUser(user);

                var bestMatch = _supervisor.GetBestChoice(user.Login, _inQueue.Where(item => item != user).ToArray().Select(item => item.Login));
                var bestBotBot = _inQueue.FirstOrDefault(bot => bot.Login == bestMatch);

                if (bestBotBot == null)
                    _logger.LogError($"{GetType().Name}: {_supervisor.GetType()} gave me an invalid GUID as best choice!");
                else
                {
                    _logger.LogDebug($"Found a match (Queue very empty for a longer time, matching with bot: {bestBotBot})");
                    OnSuggested?.Invoke(this, new MatchCreatedArgs(this, user, bestBotBot));
                    return;
                }
            }


            if (!user.IsBot)
                _lastQueuerTime = time;

            AddUser(user);
            var autoBestMatch = _inQueue.FirstOrDefault(usr => usr != user && !usr.HasEverPlayedAgainst(user, _supervisor));
            if (autoBestMatch != null)
            {
                _logger.LogDebug($"Found a match (never played against {autoBestMatch})");
                OnSuggested?.Invoke(this, new MatchCreatedArgs(this, autoBestMatch, user));
            }
        }

        public void LeaveQueue(IServerUser user)
        {
            _inQueue.Remove(user);
            _logger.LogDebug($"{user} left matchmaking");
        }

        public void LeaveQueueCompletely(IServerUser user)
        {
            _inQueue.RemoveAll(usr => usr == user);
            _logger.LogDebug($"{user} left matchmaking completely");
        }

        private void AddUser(IServerUser user)
        {
            _inQueue.Add(user);
            _joinedQueueTime[user] = _time.Elapsed.TotalMilliseconds;
            _logger.LogDebug($"{user} entered matchmaking");
        }
    }
}