using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Evaders.CommonNetworking.CommonPayloads
{
    using Newtonsoft.Json;

    public class UserState
    {
        [JsonProperty]
        public readonly bool IsQueued;

        [JsonProperty]
        public readonly bool IsIngame;

        [JsonProperty]
        public readonly bool IsPassiveBot;

        [JsonProperty]
        public readonly string Username;

        [JsonProperty]
        public readonly bool FullGameState;

        [JsonConstructor]
        public UserState(bool isQueued, bool isIngame, bool isPassiveBot, string username, bool fullGameState)
        {
            IsQueued = isQueued;
            IsIngame = isIngame;
            IsPassiveBot = isPassiveBot;
            Username = username;
            FullGameState = fullGameState;
        }

        public override string ToString()
        {
            if (IsQueued && IsIngame)
                return "Schrödingers Katze";
            if (IsQueued)
                return "Queued";
            if (IsIngame)
                return "Ingame";
            return "Idle";
        }
    }
}
