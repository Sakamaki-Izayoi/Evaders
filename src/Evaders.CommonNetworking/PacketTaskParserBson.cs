namespace Evaders.CommonNetworking
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;

    public class PacketTaskParserBson<T> : PacketParserBson<T> where T : Packet
    {
        public PacketTaskParserBson(ILogger logger) : base(logger)
        {
        }

        protected override void RunTask(Action task)
        {
            Task.Run(task);
        }
    }
}