namespace Evaders.CommonNetworking
{
    using Core.Utility;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    public class Packet
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public enum PacketTypeC2S
        {
            Authorize,
            GameAction,
            TurnEnd,
            EnterQueue,
            LeaveQueue,
            ForceResync,
            SwitchQueueMode
        }

        [JsonConverter(typeof(StringEnumConverter))]
        public enum PacketTypeS2C
        {
            AuthState,
            GameAction,
            IllegalAction,
            NextRound,
            GameState,
            GameEnd,
            QueueState
        }

        [JsonProperty]
        public object Payload;

        public int TypeNum;

        public T GetPayload<T>()
        {
            if (Payload == null)
                return default(T);
            if (Payload is T)
                return (T)Payload;
            return JsonNet.Deserialize<T>(Payload.ToString());
        }
    }
}