using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Evaders.CommonNetworking.CommonPayloads
{
    using Newtonsoft.Json;

    public class QueueState
    {
        [JsonProperty]
        public readonly string GameMode;

        [JsonConstructor]
        public QueueState(string gameMode)
        {
            GameMode = gameMode;
        }
    }
}
