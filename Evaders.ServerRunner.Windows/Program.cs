namespace Evaders.ServerRunner.Windows
{
    using System;
    using System.ServiceProcess;
    using System.Threading;
    using Core.Utility;
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
                ServiceBase[] ServicesToRun;
                ServicesToRun = new ServiceBase[] {};
                ServiceBase.Run(ServicesToRun);
                return;
            }

            var config = ServerConfiguration.Default;
            var logger = new ConsoleLogger(Severity.Info);
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