namespace Evaders.Core.Tests
{
    using System.Collections.Generic;
    using Game;

    internal class DummyGame : DefaultSandboxGame<DummyUser>
    {
        public DummyGame(IEnumerable<DummyUser> users, GameSettings settings) : base(users, settings)
        {
        }

        protected override void OnActionExecuted(DummyUser @from, GameAction action)
        {
        }

        protected override void OnTurnEnded()
        {
        }

        protected override void OnGameEnd()
        {
        }

        public void AddGameAction(DummyUser from, GameAction action)
        {
            AddAction(from, action);
        }

        public void DoNextTurn()
        {
            NextTurn();
        }

        protected override void OnIllegalAction(DummyUser user, string warningMsg)
        {
            throw new TestGameException();
        }

        protected override bool BeforeHandleAction(DummyUser @from, GameAction action)
        {
            return true;
        }
    }
}