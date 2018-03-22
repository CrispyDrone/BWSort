using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ReplayParser.Interfaces
{
    public interface IReplayMap
    {
        string MapName { get; }
        int MapWidth { get; }
        int MapHeight { get; }
    }
}
