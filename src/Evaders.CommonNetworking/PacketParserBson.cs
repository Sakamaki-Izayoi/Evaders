namespace Evaders.CommonNetworking
{
    using System;
    using System.IO;
    using Core.Utility;
    using Microsoft.Extensions.Logging;

    public class PacketParserBson<T> : PacketParser<T> where T : Packet
    {
        public PacketParserBson(ILogger logger) : base(logger)
        {
        }

        protected override void RunTask(Action task)
        {
            task();
        }

        public override T ToPacket(MemoryStream stream)
        {
            stream.Position = 0;
            return JsonNet.DeserializeBson<T>(stream);
        }

        public override byte[] FromPacket<TSource>(TSource packet)
        {
            return JsonNet.SerializeBson(packet);
        }
    }
}