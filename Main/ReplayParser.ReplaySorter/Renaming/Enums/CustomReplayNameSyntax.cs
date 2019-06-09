namespace ReplayParser.ReplaySorter.Renaming
{
    public enum CustomReplayNameSyntax
    {
        None= 0,
        WinningRaces = 1,
        LosingRaces = 2,
        Races = 4,
        WinningTeams = 8,
        LosingTeams = 16,
        Teams = 32,
        MapShort = 64,
        MapLong = 128,
        Matchup = 256,
        Date = 512,
        DateTime = 1024,
        DurationShort = 2048,
        DurationLong = 4096,
        GameFormat = 8192,
        GameTypeShort = 16384,
        GameTypeLong = 32768,
        PlayerInfo = 65536,
        PlayersWithObservers = 131072,
        Players = 262144,
        PlayerX = 524288,
        PlayerXRaceShort = 1048576,
        PlayerXRaceLong = 2097152,
        PlayerXVictoryStatusShort = 4194304,
        PlayerXVictoryStatusLong = 8388608,
        OriginalName = 16777216,
        CounterShort = 33554432,
        CounterLong = 67108864
    }
}
