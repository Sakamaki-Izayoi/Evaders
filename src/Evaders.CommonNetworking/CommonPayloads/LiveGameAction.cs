using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Evaders.CommonNetworking.CommonPayloads
{
    using Core.Game;
    using Core.Utility;
    using Newtonsoft.Json;

    public class LiveGameAction : GameAction
    {
        [JsonProperty]
        public readonly int Turn;

        [JsonConstructor]
        public LiveGameAction(GameActionType type, Vector2 position, long controlledEntityIdentifier, int turn = -1) : base(type, position, controlledEntityIdentifier)
        {
            Turn = turn;
        }
    }
}
