using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Evaders.Server
{
    public interface IWinStatistics
    {
        int Wins { get; }
        int Losses { get; }
    }
}
