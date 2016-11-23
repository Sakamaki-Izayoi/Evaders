namespace ExampleBot
{
    internal interface IContextManager
    {
        void Add(Context context);
        int Count<T>() where T : Context;
        bool Has<T>() where T : Context;
    }
}