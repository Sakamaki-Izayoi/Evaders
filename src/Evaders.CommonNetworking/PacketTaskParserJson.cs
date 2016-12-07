namespace Evaders.CommonNetworking
{
    using System;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;

    public class PacketTaskParser<T> : PacketParserJson<T> where T : Packet
    {
        public PacketTaskParser(ILogger logger, Encoding jsonEncoding) : base(logger, jsonEncoding)
        {
        }

        protected override void RunTask(Action task)
        {
            Task.Run(task);
        }
    }
}