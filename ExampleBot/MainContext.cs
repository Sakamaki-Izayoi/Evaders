namespace ExampleBot
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Windows.Forms;
    using Evaders.Client;
    using Evaders.Core.Game;
    using Microsoft.Xna.Framework;
    using Vector2 = Evaders.Core.Utility.Vector2;

    internal class MainContext : Context
    {
        private readonly IQueuer _queuer;
        private readonly Random _rnd = new Random(BitConverter.ToInt32(Guid.NewGuid().ToByteArray(), 0));
        private readonly HashSet<long> _ignoredProjectiles = new HashSet<long>();

        public MainContext(IContextManager contextManager, IQueuer queuer) : base(contextManager)
        {
            _queuer = queuer;
            _queuer.OnLeftGame += ConsiderQueueing;
            _queuer.OnJoinedGame += (sender, args) => args.Game.OnWaitingForActions += GameTurn;
            _queuer.EnterQueue();
        }

        private void GameTurn(object sender, GameEventArgs gameEventArgs)
        {
            var game = gameEventArgs.Game;
            foreach (var entity in game.MyEntities)
            {
                var assumedWaypoint = entity.MovingTo;
                List<Projectile> minHits = null;
                var minHitCount = int.MaxValue;
                const int iterationsCount = 1000;

                for (var iterations = 0; iterations <= iterationsCount; iterations++)
                {
                    var currentHits = game.EnemyProjectiles.Where(proj => !_ignoredProjectiles.Contains(proj.ProjectileIdentifier) && WillHit(proj, entity, assumedWaypoint)).ToList();
                    if (!currentHits.Any()) break;
                    if (currentHits.Count < minHitCount)
                    {
                        minHitCount = currentHits.Count;
                        minHits = currentHits;
                    }

                    var angle = MathHelper.ToRadians(_rnd.Next(0, 360));
                    var dst = _rnd.Next(0, (int)game.Settings.ArenaRadius);
                    assumedWaypoint = new Vector2(dst * Math.Sin(angle), dst * Math.Cos(angle));

                    if (iterations == iterationsCount)
                    {
                        foreach (var l in minHits.Select(item => item.ProjectileIdentifier))
                        {
                            _ignoredProjectiles.Add(l);
                        }
                    }
                }
                if (assumedWaypoint.Distance(entity.MovingTo) > double.Epsilon)
                    entity.MoveTo(assumedWaypoint);

                if (entity.CanShoot && game.EnemyEntities.Any())
                {
                    var enemy = game.EnemyEntities.First();
                    if (_rnd.Next(0, 2) == 0)
                    {
                        entity.Shoot(enemy.Position);
                    }
                    else
                    {
                        var turns = entity.GetNeededProjectileTurns(enemy.Position);
                        entity.Shoot(enemy.GetPositionIn((uint)turns).ExtendedAway(entity.Position, 1337d));
                    }

                }
            }
            Console.WriteLine("Turn " + game.Turn);
        }

        private static bool WillHit(Projectile proj, EntityBase entity, Vector2 assumedEntityWaypoint)
        {
            for (var i = 1u; i < 100; i++)
            {
                if (proj.WillHitIn(entity, i, assumedEntityWaypoint))
                    return true;
            }
            return false;
        }

        private void ConsiderQueueing(object sender, GameEventArgs gameEventArgs)
        {
            if (_queuer.CurrentlyRunningGames + _queuer.LastServersideQueueCount < 1)
                _queuer.EnterQueue();
        }
    }
}