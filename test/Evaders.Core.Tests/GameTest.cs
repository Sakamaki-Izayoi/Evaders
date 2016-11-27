namespace Evaders.Core.Tests
{
    using System;
    using System.Linq;
    using Game;
    using NUnit.Framework;
    using Utility;

    [TestFixture]
    internal class GameTest
    {
        private static CharacterData TestCharData => new CharacterData(100, 10, 10, 0.5, 30, 300d, 75d);
        private static GameSettings TestGameSettings => new GameSettings(100f, 30, 1f, 10f, TestCharData, 100f, 100f*30, TestCharData.MaxHealth);

        [Test]
        public void ArenaShrinking()
        {
            var game = new DummyGame(new[] {new DummyUser(true, 0)}, TestGameSettings);

            game.DoNextTurn();
            Assert.AreEqual(1, game.ValidEntitesControllable.Count(), "Arena shrank instantly / Shrinking start time not applied");

            do
            {
                game.DoNextTurn();
            } while (game.Turn < game.Settings.ArenaShrinkStartTurn);

            Assert.AreEqual(1, game.ValidEntitesControllable.Count(), "Entity gone before arena shrinking");

            game.DoNextTurn();

            Assert.AreEqual(0, game.ValidEntitesControllable.Count(), "Arena shrinking not working / damage not applied");

            Assert.AreEqual(true, game.GameEnded, "Game did not end from arena shrinking");
        }

        [Test]
        public void BasicTurn()
        {
            var game = new DummyGame(new[] {new DummyUser(true, 0), new DummyUser(true, 1)}, TestGameSettings);
            foreach (var validEntity in game.ValidEntitesControllable)
            {
                validEntity.MoveTo(validEntity.Position + new Vector2(100, 0));
                validEntity.Shoot(validEntity.Position + new Vector2(-100, 0));
            }
            game.DoNextTurn();
        }

        [Test]
        public void CanMove()
        {
            var game = new DummyGame(new[] {new DummyUser(true, 0), new DummyUser(true, 1)}, TestGameSettings);
            var firstEntity = game.ValidEntitesControllable.First();

            Assert.Greater(firstEntity.CharData.SpeedSec, 0d, "Invalid speed, cannot test movement");

            firstEntity.MoveTo(game.ValidEntities.Last().Position);
            var pos = firstEntity.Position;
            game.DoNextTurn();

            Assert.LessOrEqual(firstEntity.Position.Distance(pos) - firstEntity.CharData.SpeedSec, double.Epsilon, "Did not move as far as expected / didn't move at all");
        }

        [Test]
        public void CanShoot()
        {
            var game = new DummyGame(new[] {new DummyUser(true, 0), new DummyUser(true, 1)}, TestGameSettings);
            game.ValidEntitesControllable.First().Shoot(game.ValidEntities.Last().Position);
            game.DoNextTurn();

            Assert.AreEqual(1, game.ValidProjectiles.Count(), "Projectiles do not spawn");

            var projectilePos = game.ValidProjectiles.First().Position;
            game.DoNextTurn();
            Assert.LessOrEqual((game.ValidProjectiles.First().Position - projectilePos).Length - game.Settings.DefaultCharacterData.ProjectileSpeedSec, double.Epsilon, "Projectiles do not move");
        }

        [Test]
        public void GameEnd()
        {
            var game = new DummyGame(new[] {new DummyUser(true, 0), new DummyUser(true, 1)}, TestGameSettings);
            var sourceEntity = game.ValidEntitesControllable.First();
            var targetEntity = game.ValidEntitesControllable.Last();

            Assert.AreNotEqual(sourceEntity, targetEntity);
            Assert.Greater(sourceEntity.ReloadFrames, 0);

            var shotsForKill = (int) Math.Ceiling(targetEntity.Health/(double) sourceEntity.CharData.ProjectileDamage);
            var travelTimeSec = (sourceEntity.Position.Distance(targetEntity.Position) - (sourceEntity.CharData.HitboxSize + sourceEntity.CharData.ProjectileHitboxSize*2 + targetEntity.CharData.HitboxSize))/sourceEntity.CharData.ProjectileSpeedSec;
            var travelFrames = (int) (travelTimeSec/game.TimePerFrameSec);

            var expectedGameFrames = (shotsForKill - 1)*sourceEntity.ReloadFrames + travelFrames;


            for (var i = 0; !game.GameEnded; i++)
            {
                if (sourceEntity.CanShoot)
                    sourceEntity.Shoot(targetEntity.Position);
                game.DoNextTurn();

                Assert.LessOrEqual(i, expectedGameFrames, "Game didn't end yet but should have");
            }
            Assert.AreEqual(expectedGameFrames, game.Turn, "Game was ended faster than expected");
        }

        [Test]
        public void InvalidActionDetected()
        {
            var game = new DummyGame(new[] {new DummyUser(true, 0), new DummyUser(true, 1)}, TestGameSettings);
            game.AddGameAction(game.Users.FirstOrDefault(), new GameAction((GameActionType) 1337, new Vector2(0, 0), game.ValidEntities.First().EntityIdentifier));
            Assert.Throws<TestGameException>(() => game.DoNextTurn(), "Game doesn't properly validate game action");
        }

        [Test]
        public void ProjectilesDespawn()
        {
            var game = new DummyGame(new[] {new DummyUser(true, 0)}, TestGameSettings);
            var entity = game.ValidEntitesControllable.First();
            entity.Shoot(entity.Position + new Vector2(100, 0));
            var despawnFrame = (int) Math.Ceiling(game.Settings.ProjectileLifeTimeSec/game.TimePerFrameSec);

            Assert.Greater(game.Settings.ProjectileLifeTimeSec, 0);

            while (game.Turn < despawnFrame)
                game.DoNextTurn();

            Assert.AreEqual(1, game.ValidProjectiles.Count(), "Projectile despawned too early");

            game.DoNextTurn();

            Assert.AreEqual(0, game.ValidProjectiles.Count(), "Projectile not despawning / despawning too late");
        }

        [Test]
        public void ZeroDistanceShotDetected()
        {
            var game = new DummyGame(new[] {new DummyUser(true, 0), new DummyUser(true, 1)}, TestGameSettings);
            var entity = game.ValidEntitesControllable.First();
            entity.Shoot(entity.Position);
            Assert.Throws<TestGameException>(() => game.DoNextTurn(), "Can shoot at my position (could cause invalid unit vector)");
        }
    }
}