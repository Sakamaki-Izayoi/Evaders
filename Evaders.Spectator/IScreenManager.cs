using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Evaders.Spectator
{
    public enum SeeThrough { Fully, Partial, None }

    public interface IScreenManager
    {
        void Add(Screen screen);
        void Remove(Screen screen);
    }
}
