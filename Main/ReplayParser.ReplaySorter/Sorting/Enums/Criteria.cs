using System;

namespace ReplayParser.ReplaySorter
{
    [Flags]
    public enum Criteria
    {
        PLAYERNAME = 1,
        GAMETYPE = 2,
        MATCHUP = 4,
        MAP = 8,
        DURATION = 16
    }
}