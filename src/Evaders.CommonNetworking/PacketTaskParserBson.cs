using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Evaders.CommonNetworking
{
    using System.IO;
    using Core.Utility;
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
