namespace Evaders.Client
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using CommonNetworking;
    using CommonNetworking.CommonPayloads;
    using Core.Game;
    using Newtonsoft.Json;

    public class ClientGame : Game<ClientUser>
    {
        public event Action OnGameEnded;
        public event EventHandler<GameEventArgs> OnWaitingForActions;
        public IEnumerable<Entity> MyEntities => EntitiesInternal.Where(entity => entity.PlayerIdentifier == MyPlayerIdentifier);
        public IEnumerable<EntityBase> EnemyEntities => EntitiesInternal.Where(entity => entity.PlayerIdentifier != MyPlayerIdentifier);
        public IEnumerable<Projectile> EnemyProjectiles => Projectiles.Where(projectile => projectile.PlayerIdentifier != MyPlayerIdentifier);
        public IEnumerable<Projectile> MyProjectiles => Projectiles.Where(projectile => projectile.PlayerIdentifier == MyPlayerIdentifier);
        public long GameIdentifier { get; private set; }
        public long MyPlayerIdentifier { get; private set; }
        private Connection _connection;

        internal ClientGame(IEnumerable<ClientUser> users, GameSettings settings, IMapGenerator generator, Connection connection, long myPlayerIdentifier, long gameIdentifier) : base(users, settings, generator)
        {
            _connection = connection;
            MyPlayerIdentifier = myPlayerIdentifier;
            GameIdentifier = gameIdentifier;
        }

        [JsonConstructor]
        private ClientGame(IEnumerable<ClientUser> users, GameSettings settings, IEnumerable<Entity> entities, IEnumerable<Projectile> projectiles, IEnumerable<HealOrbSpawn> healSpawns, CloneOrbSpawn clonerSpawn, long lastEntityIdentifier, long lastProjectileIdentifier) : base(users, settings, entities, projectiles, healSpawns, clonerSpawn, lastEntityIdentifier, lastProjectileIdentifier)
        {
        }

        internal void SetGameDetails(long myPlayerIdentifier, long gameIdentifier, Connection connection)
        {
            MyPlayerIdentifier = myPlayerIdentifier;
            GameIdentifier = gameIdentifier;
            _connection = connection;
        }

        protected override void OnActionExecuted(ClientUser from, GameAction action)
        {
        }

        protected override void OnTurnEnded()
        {
            RequestClientActions();
        }

        protected override void OnGameEnd()
        {
            OnGameEnded?.Invoke();
        }

        internal void RequestClientActions()
        {
            OnWaitingForActions?.Invoke(this, new GameEventArgs(this));
            _connection.Send(Packet.PacketTypeC2S.TurnEnd, new TurnEnd(Turn));
        }

        protected override void OnIllegalAction(ClientUser user, string warningMsg)
        {
            throw new GameException($"Source: Local (Client), Motd: {warningMsg}");
        }

        internal void DoNextTurn()
        {
            NextTurn();
        }

        internal ClientUser GetOwnerOfEntity(long entityIdentifier)
        {
            var entity = EntitiesInternal.FirstOrDefault(item => item.EntityIdentifier == entityIdentifier);
            if (entity == null)
                return null;
            return Users.FirstOrDefault(user => user.Identifier == entity.PlayerIdentifier);
        }

        internal void AddActionWithoutNetworking(ClientUser from, GameAction action)
        {
            if (from == null)
                throw new ArgumentException("User cannot be null", nameof(from));
            AddActionInternal(from, action);
        }

        protected override bool AddAction(ClientUser from, GameAction action)
        {
            if (!BeforeHandleAction(from, action))
                return false;

            _connection.Send(Packet.PacketTypeC2S.GameAction, new LiveGameAction(action.Type, action.Position, action.ControlledEntityIdentifier, Turn));
            return true;
        }

        protected override bool BeforeHandleAction(ClientUser from, GameAction action)
        {
            return true;
        }
    }
}