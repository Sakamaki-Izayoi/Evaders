namespace Evaders.ServerRunner.Windows
{
    using System.ServiceProcess;

    internal static class Program
    {
        /// <summary>
        ///     The main entry point for the application.
        /// </summary>
        private static void Main()
        {
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
            };
            ServiceBase.Run(ServicesToRun);
        }
    }
}