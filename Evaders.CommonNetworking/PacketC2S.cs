namespace Evaders.CommonNetworking
{
    using Newtonsoft.Json;

    public class PacketC2S : Packet
    {
        [JsonProperty]
        public PacketTypeC2S Type => (PacketTypeC2S) TypeNum;

        [JsonConstructor]
        public PacketC2S(PacketTypeC2S type, object payload)
        {
            TypeNum = (int) type;
            Payload = payload;
        }

        public override string ToString()
        {
            return $"{Type}: {Payload}";
        }
    }
}