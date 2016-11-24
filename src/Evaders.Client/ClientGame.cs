namespace Evaders.Client
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using CommonNetworking;
    using Core.Game;
    using Extensions;
    using Newtonsoft.Json;

    public class ClientGame : Game<ClientUser>
    {
        public event Action OnGameEnded;
        public event EventHandler<MessageEventArgs> OnServerRejectedAction;
        public event EventHandler<GameEventArgs> OnWaitingForActions;
        public IEnumerable<Entity> MyEntities => Entities.Where(entity => entity.PlayerIdentifier == _myPlayerIdentifier);
        public IEnumerable<EntityBase> EnemyEntities => Entities.Where(entity => entity.PlayerIdentifier != _myPlayerIdentifier);
        public IEnumerable<Projectile> EnemyProjectiles => ValidProjectiles.Where(projectile => projectile.PlayerIdentifier != _myPlayerIdentifier);
        public IEnumerable<Projectile> MyProjectiles => ValidProjectiles.Where(projectile => projectile.PlayerIdentifier == _myPlayerIdentifier);
        public long GameIdentifier { get; private set; }
        private Connection _connection;
        private long _myPlayerIdentifier;

        internal ClientGame(IEnumerable<ClientUser> users, GameSettings settings, Connection connection, long myPlayerIdentifier, long gameIdentifier) : base(users, settings)
        {
            _connection = connection;
            _myPlayerIdentifier = myPlayerIdentifier;
            GameIdentifier = gameIdentifier;
        }

        [JsonConstructor]
        private ClientGame(IEnumerable<ClientUser> users, GameSettings settings, IEnumerable<Entity> entities) : base(users, settings)
        {
            Entities.Clear();
            Entities.AddRange(entities);
        }

        internal void SetGameDetails(long myPlayerIdentifier, long gameIdentifier, Connection connection)
        {
            _myPlayerIdentifier = myPlayerIdentifier;
            GameIdentifier = gameIdentifier;
            _connection = connection;
        }

        protected override void OnActionExecuted(ClientUser @from, GameAction action)
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
            _connection.Send(Packet.PacketTypeC2S.TurnEnd, GameIdentifier);
        }

        internal void HandleServerIllegalAction(string msg)
        {
            OnServerRejectedAction?.Invoke(this, new MessageEventArgs(msg));
        }

        protected override void OnIllegalAction(ClientUser user, string warningMsg)
        {
            throw new GameException($"Source: Local (Client), Message: {warningMsg}");
        }

        private void EndTurn()
        {
            _connection.Send(Packet.PacketTypeC2S.TurnEnd, GameIdentifier);
        }

        internal void DoNextTurn()
        {
            NextTurn();
        }

        internal ClientUser GetOwnerOfEntity(long entityIdentifier)
        {
            var entity = Entities.FirstOrDefault(item => item.EntityIdentifier == entityIdentifier);
            if (entity == null)
                return null;
            return Users.FirstOrDefault(user => user.Identifier == entity.PlayerIdentifier);
        }

        internal void AddActionWithoutNetworking(ClientUser @from, GameAction action)
        {
            if (@from == null)
                throw new ArgumentException("User cannot be null", nameof(from));
            AddActionInternal(from, action);
        }

        protected override bool AddAction(ClientUser @from, GameAction action)
        {
            if (!BeforeHandleAction(@from, action))
                return false;

            _connection.Send(Packet.PacketTypeC2S.GameAction, action.AsLiveAction(GameIdentifier));
            return true;
        }

        protected override bool BeforeHandleAction(ClientUser @from, GameAction action)
        {
            return true;
        }
    }
}