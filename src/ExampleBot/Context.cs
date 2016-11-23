namespace ExampleBot
{
    internal abstract class Context
    {
        protected readonly IContextManager ContextManager;

        public Context(IContextManager contextManager)
        {
            ContextManager = contextManager;
        }

        public virtual void ContinueWork()
        {
        }
    }
}