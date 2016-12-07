namespace Evaders.ServerRunner.Windows
{
    using System;
    using System.ServiceProcess;
    using System.Threading;
    using Core.Game;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Logging.Console;
    using Server;
    using Server.Integration;

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

            var config = new ServerSettings();
            var logger = new ConsoleLogger("console", (m, l) => l >= LogLevel.Information, true);
            var supervisor = new EmptySupervisor();
            var serv = new EvadersServer(new DefaultProviderFactory<IServerSupervisor>(item => supervisor), new DefaultProviderFactory<GameSettings>(item => GameSettings.Default), new DefaultProviderFactory<IMatchmaking>(item => new Matchmaking("Default", config.MaxTimeInQueueSec, logger)), logger, config);
            serv.Start();

            var wait = new SpinWait();
            while (true)
            {
                serv.Update();
                wait.SpinOnce();
            }
        }
    }
}