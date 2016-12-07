namespace Evaders.CommonNetworking
{
    using System;
    using System.IO;
    using System.Net.Sockets;
    using System.Text;
    using Core.Utility;
    using Microsoft.Extensions.Logging;

    public class PacketParserJson<T> : PacketParser<T> where T : Packet
    {
        private readonly Encoding _jsonEncoding;

        public PacketParserJson(ILogger logger, Encoding jsonEncoding) : base(logger)
        {
            _jsonEncoding = jsonEncoding;
        }


        protected override void RunTask(Action task)
        {
            task();
        }

        public override T ToPacket(MemoryStream stream)
        {
            ArraySegment<byte> buffer;
            stream.TryGetBuffer(out buffer);
            Console.WriteLine("C"+_jsonEncoding.GetString(buffer.Array, buffer.Offset, buffer.Count));
            return JsonNet.Deserialize<T>(_jsonEncoding.GetString(buffer.Array, buffer.Offset, buffer.Count));
        }

        public override byte[] FromPacket<TSource>(TSource packet)
        {
            Console.WriteLine("C2"+JsonNet.Serialize(packet));
            return _jsonEncoding.GetBytes(JsonNet.Serialize(packet));
        }
    }
}