using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReplayParser.ReplaySorter
{
    public static class Constants
    {
        public static readonly double SlowestFPS    = (double) 1000 / 167;
        public static readonly double SlowerFPS     = (double) 1000 / 111;
        public static readonly double SlowFPS       = (double) 1000 / 83;
        public static readonly double NormalFPS     = (double) 1000 / 67;
        public static readonly double FastFPS       = (double) 1000 / 56;
        public static readonly double FasterFPS     = (double) 1000 / 48;
        public static readonly double FastestFPS    = (double) 1000 / 42;
    }
}
