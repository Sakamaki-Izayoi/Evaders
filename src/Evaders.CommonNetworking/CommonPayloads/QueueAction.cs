using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Evaders.CommonNetworking.CommonPayloads
{
    using Newtonsoft.Json;

    public class QueueAction
    {
        [JsonProperty]
        public readonly string GameMode;

        [JsonProperty]
        public readonly int Count;

        [JsonConstructor]
        public QueueAction(string gameMode, int count)
        {
            GameMode = gameMode;
            Count = count;
        }
    }
}
