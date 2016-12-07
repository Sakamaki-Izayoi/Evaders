namespace Evaders.CommonNetworking
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;

    public class PacketTaskParserBson<T> : PacketParserBson<T> where T : Packet
    {
        public PacketTaskParserBson(ILogger logger) : base(logger)
        {
            throw new Exception("Did you fix the concurrency problem? (wrong packet orders fuck you up)");
        }

        protected override void RunTask(Action task)
        {
            Task.Run(task);
        }
    }
}