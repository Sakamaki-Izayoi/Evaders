namespace Evaders.Client
{
    using System;

    public class CountChangedEventArgs : EventArgs
    {
        public readonly int Count;

        public CountChangedEventArgs(int count)
        {
            Count = count;
        }
    }
}