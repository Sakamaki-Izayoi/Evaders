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
        private static CharacterData TestCharData => new CharacterData(100, 10, 10, 0.5, 30, 300d, 75d, 100);
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
            } while (game.Turn < game.Settings.ArenaShrinkStartTurn); // one turn before shrinking starts

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
        // ReSharper disable once InconsistentNaming
        public void EntityAPI()
        {
            var game = new DummyGame(new[] {new DummyUser(true, 0), new DummyUser(true, 1)}, TestGameSettings);
            var entity = game.ValidEntitesControllable.First();

            var target = game.ValidEntities.First(ent => ent.EntityIdentifier != entity.EntityIdentifier);
            Assert.AreNotEqual(entity, target, "wtf");

            entity.Shoot(entity.Position + new Vector2(100, 0));
            game.DoNextTurn();

            var shootTurn = entity.NextReloadedTurn;

            while (!entity.CanShoot)
                game.DoNextTurn();

            Assert.AreEqual(shootTurn, game.Turn, $"Incorrect API: {nameof(entity.NextReloadedTurn)}");
            Assert.AreEqual(entity.ReloadFrames, game.Turn, $"Incorrect API: {nameof(entity.ReloadFrames)}");

            // Todo
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
        public void ProjectilesDealAreaDamage()
        {
            var game = new DummyGame(new[] {new DummyUser(true, 0), new DummyUser(true, 1)}, TestGameSettings);
            var entity = game.ValidEntitesControllable.First();

            var target = game.ValidEntities.First(ent => ent.EntityIdentifier != entity.EntityIdentifier);
            Assert.AreNotEqual(entity, target, "wtf");

            var target2 = game.AddEntity(target.Position, target.PlayerIdentifier, target.CharData);
            Assert.AreEqual(3, game.ValidEntities.Count(), "Invalid amount of entities in test, maybe adding the dummy entity didn't work?");

            entity.Shoot(target.Position);

            for (var i = 0; i < 10000; i++)
            {
                game.DoNextTurn();

                if (target.Health != target.CharData.MaxHealth)
                {
                    Assert.AreEqual(target.Health, target2.Health, "AOE damage does not work: health of targets not equal");
                    return;
                }
            }
            Assert.Fail("Projectile never detonated or references not valid");
        }

        [Test]
        public void ProjectilesDespawn()
        {
            var game = new DummyGame(new[] {new DummyUser(true, 0)}, TestGameSettings);
            var entity = game.ValidEntitesControllable.First();
            entity.Shoot(entity.Position + new Vector2(100, 0));
            var despawnTurn = (int) Math.Ceiling(game.Settings.ProjectileLifeTimeSec/game.TimePerFrameSec);

            Assert.Greater(game.Settings.ProjectileLifeTimeSec, 0);

            while (game.Turn < despawnTurn)
                game.DoNextTurn();

            Assert.AreEqual(1, game.ValidProjectiles.Count(), "Projectile despawned too early");
            Assert.AreEqual(game.ValidProjectiles.First().LifeEndTurn, despawnTurn, "Projectile LifeEndTurn is incorrect");
            Assert.AreEqual(despawnTurn, game.Turn, "Despawn turn incorrect");

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