namespace Evaders.ServerRunner.Windows
{
    using System;
    using System.ServiceProcess;
    using System.Threading;
    using Core.Utility;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Logging.Console;
    using Server;

    internal static class Program
    {
        /// <summary>
        ///     The main entry point for the application.
        /// </summary>
        private static void Main()
        {
            if (!Environment.UserInteractive)
            {
                throw new NotImplementedException();
#pragma warning disable 162
                ServiceBase[] ServicesToRun;
                ServicesToRun = new ServiceBase[] {};
                ServiceBase.Run(ServicesToRun);
                return;
#pragma warning restore 162
            }

            var config = ServerConfiguration.Default;
            var logger = new ConsoleLogger("console", (m, l) => l >= LogLevel.Information, true);
            var supervisor = new EmptySupervisor();
            var serv = new EvadersServer(supervisor, new Matchmaking(config.MaxTimeInQueueSec, logger, supervisor), logger, config);

            var wait = new SpinWait();
            while (true)
            {
                serv.Update();
                wait.SpinOnce();
            }
        }
    }
}