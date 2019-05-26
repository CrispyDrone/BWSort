namespace ReplayParser.ReplaySorter.CustomFormat
{
    public enum CustomReplayNameSyntax
    {
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
        Players = 131072,
        PlayerX = 262144,
        PlayerXRaceShort = 524288,
        PlayerXRaceLong = 1048576,
        PlayerXVictoryStatus = 2097152
    }
}
