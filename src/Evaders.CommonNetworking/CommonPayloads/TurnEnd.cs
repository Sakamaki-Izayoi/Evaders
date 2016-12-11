using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Evaders.CommonNetworking.CommonPayloads
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Serialization;

    public class TurnEnd
    {
        [JsonProperty]
        public readonly int Turn;

        [JsonConstructor]
        public TurnEnd(int turn)
        {
            Turn = turn;
        }
    }
}
