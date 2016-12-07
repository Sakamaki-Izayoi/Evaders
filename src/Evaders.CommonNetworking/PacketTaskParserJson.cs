namespace Evaders.CommonNetworking
{
    using System;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;

    public class PacketTaskParserJson<T> : PacketParserJson<T> where T : Packet
    {
        public PacketTaskParserJson(ILogger logger, Encoding jsonEncoding) : base(logger, jsonEncoding)
        {
            throw new Exception("Did you fix the concurrency problem? (wrong packet orders fuck you up)");
        }

        protected override void RunTask(Action task)
        {
            Task.Run(task);
        }
    }
}