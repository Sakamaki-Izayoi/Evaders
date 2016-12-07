namespace ExampleBot
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using Evaders.Spectator.OpenGL;

    internal class Program : IContextManager
    {
        private readonly List<Context> _contexts = new List<Context>();

        public void Add(Context context)
        {
            _contexts.Add(context);
        }

        public int Count<T>() where T : Context
        {
            return _contexts.Count(item => item is T);
        }

        public bool Has<T>() where T : Context
        {
            return _contexts.Any(item => item is T);
        }

        [STAThread]
        private static void Main()
        {
            var program = new Program();
            var context = new ConnectionContext(IPAddress.Loopback, 9090, program);
            program.Add(context);

            var visualizer = new SpectatorWindow();
            visualizer.Add(new ScreenGameRenderer(visualizer, context.Connection));
            visualizer.PostUpdate += program.Update;
            visualizer.Run();
        }

        public void Update()
        {
            for (var index = 0; index < _contexts.Count; index++)
            {
                var context = _contexts[index];
                context.ContinueWork();
            }
        }
    }
}