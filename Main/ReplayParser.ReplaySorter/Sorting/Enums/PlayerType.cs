using System;

namespace ReplayParser.ReplaySorter.Sorting
{
    [Flags]
    public enum PlayerType
    {
        None = 0,
        Winner = 1,
        Loser = 2,
        Observer = 4
    }
}