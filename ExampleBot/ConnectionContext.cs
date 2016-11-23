namespace ExampleBot
{
    using System;
    using System.Diagnostics;
    using System.Net;
    using Evaders.Client;

    internal class ConnectionContext : Context
    {
        public readonly Connection Connection;

        public ConnectionContext(IPAddress address, ushort port, IContextManager manager) : base(manager)
        {
            Connection = new Connection(Guid.NewGuid(), "nin0", address, port, new ConsoleLogger());
            Connection.OnLoggedIn += (sender, args) =>
            {
                Console.WriteLine("SERVER: " + args.Message);
                ContextManager.Add(new MainContext(ContextManager, args.Queuer));
            };
            Connection.OnIllegalAction += (sender, s) => Console.WriteLine("ILLEGAL: " + s.Message);
            Connection.OnKicked += (sender, s) =>
            {
                Console.WriteLine("KICKED: " + s.Message);
                Debugger.Break();
                Environment.Exit(0);
            };
        }


        public override void ContinueWork()
        {
            Connection.Update();
        }
    }
}