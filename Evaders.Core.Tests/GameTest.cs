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
        private static GameSettings TestGameSettings => new GameSettings(100f, 30, 1f, 10f, TestCharData);

        [Test]
        public void GameBasicTurn()
        {
            var game = new DummyGame(new[] {new DummyUser(true, 0), new DummyUser(true, 1)}, TestGameSettings);
            foreach (var validEntity in game.ValidEntities)
            {
                validEntity.MoveTo(validEntity.Position + new Vector2(100, 0));
                validEntity.Shoot(validEntity.Position + new Vector2(-100, 0));
            }
            game.NextTurn();
        }

        [Test]
        public void GameCanMove()
        {
            var game = new DummyGame(new[] {new DummyUser(true, 0), new DummyUser(true, 1)}, TestGameSettings);
            var firstEntity = game.ValidEntities.First();

            Assert.Greater(firstEntity.CharData.SpeedSec, 0d, "Invalid speed, cannot test movement");

            firstEntity.MoveTo(game.ValidEntities.Last().Position);
            var pos = firstEntity.Position;
            game.NextTurn();

            Assert.LessOrEqual(firstEntity.Position.Distance(pos) - firstEntity.CharData.SpeedSec, double.Epsilon, "Did not move as far as expected / didn't move at all");
        }

        [Test]
        public void GameCanShoot()
        {
            var game = new DummyGame(new[] {new DummyUser(true, 0), new DummyUser(true, 1)}, TestGameSettings);
            game.ValidEntities.First().Shoot(game.ValidEntities.Last().Position);
            game.NextTurn();

            Assert.AreEqual(game.ValidProjectiles.Count, 1, "Projectiles do not spawn");

            var projectilePos = game.ValidProjectiles.First().Position;
            game.NextTurn();
            Assert.LessOrEqual((game.ValidProjectiles.First().Position - projectilePos).Length - game.Settings.DefaultCharacterData.ProjectileSpeedSec, double.Epsilon, "Projectiles do not move");
        }

        [Test]
        public void GameEnd()
        {
            var game = new DummyGame(new[] {new DummyUser(true, 0), new DummyUser(true, 1)}, TestGameSettings);
            var sourceEntity = game.ValidEntities.First();
            var targetEntity = game.ValidEntities.Last();

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
                game.NextTurn();

                Assert.LessOrEqual(i, expectedGameFrames, "Game didn't end yet but should have");
            }
            Assert.AreEqual(game.Frame, expectedGameFrames, "Game was ended faster than expected");
        }

        [Test]
        public void GameInvalidAction()
        {
            var game = new DummyGame(new[] {new DummyUser(true, 0), new DummyUser(true, 1)}, TestGameSettings);
            game.AddAction(game.Users.FirstOrDefault(), new GameAction((GameActionType) 1337, new Vector2(0, 0), game.ValidEntities.First().EntityIdentifier));
            Assert.Throws<TestGameException>(() => game.NextTurn(), "Game doesn't properly validate game action");
        }

        [Test]
        public void GameZeroDistanceShot()
        {
            var game = new DummyGame(new[] {new DummyUser(true, 0), new DummyUser(true, 1)}, TestGameSettings);
            var entity = game.ValidEntities.First();
            entity.Shoot(entity.Position);
            Assert.Throws<TestGameException>(() => game.NextTurn(), "Can shoot at my position (could cause invalid unit vector)");
        }

        [Test]
        public void ProjectilesDespawn()
        {
            var game = new DummyGame(new[] {new DummyUser(true, 0)}, TestGameSettings);
            var entity = game.ValidEntities.First();
            entity.Shoot(entity.Position + new Vector2(100, 0));
            var despawnFrame = (int) Math.Ceiling(game.Settings.ProjectileLifeTimeSec/game.TimePerFrameSec);

            Assert.Greater(game.Settings.ProjectileLifeTimeSec, 0);

            while (game.Frame < despawnFrame)
                game.NextTurn();

            Assert.AreEqual(game.ValidProjectiles.Count, 1, "Projectile despawned too early");

            game.NextTurn();

            Assert.AreEqual(game.ValidProjectiles.Count, 0, "Projectile not despawning / despawning too late");
        }
    }
}