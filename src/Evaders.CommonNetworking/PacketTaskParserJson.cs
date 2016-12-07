namespace Evaders.CommonNetworking
{
    using System;
    using System.IO;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading.Tasks;
    using Core.Utility;
    using Microsoft.Extensions.Logging;

    public class PacketTaskParser<T> : PacketParserJson<T> where T : Packet
    {
        public PacketTaskParser(ILogger logger, Encoding jsonEncoding) : base(logger, jsonEncoding) { }

        protected override void RunTask(Action task)
        {
            Task.Run(task);
        }
    }
}