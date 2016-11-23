namespace Evaders.CommonNetworking
{
    using Newtonsoft.Json;

    public class PacketS2C : Packet
    {
        [JsonProperty]
        public PacketTypeS2C Type => (PacketTypeS2C) TypeNum;

        [JsonConstructor]
        public PacketS2C(PacketTypeS2C type, object payload)
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