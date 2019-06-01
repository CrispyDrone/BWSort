using ReplayParser.Entities;
using ReplayParser.Interfaces;
using System.Globalization;
using System.Linq;
using System.Text;
using System;

namespace ReplayParser.ReplaySorter.Renaming
{
    public class ReplayDecorator
    {
        #region private

        #region enum

        enum OutputFormat
        {
            Short,
            Long
        }

        #endregion

        #region fields

        private IReplay _replay;

        #endregion

        #region methods

        #region other

        private string GetShortFormGameType(GameType gameType)
        {
            switch (gameType)
            {
                case GameType.Melee:
                    return "M";
                case GameType.FreeForAll:
                    return "FFA";
                case GameType.OneOnOne:
                    return "OvO";
                case GameType.CaptureTheFlag:
                    return "CTF";
                case GameType.Greed:
                    return "G";
                case GameType.Slaughter:
                    return "S";
                case GameType.SuddenDeath:
                    return "SD";
                case GameType.Ladder:
                    return "L";
                case GameType.UseMapSettings:
                    return "UMS";
                case GameType.TeamMelee:
                    return "TM";
                case GameType.TeamFreeForAll:
                    return "TMFFA";
                case GameType.TeamCaptureTheFlag:
                    return "TCTF";
                case GameType.TopVsBottom:
                    return "TvB";
                case GameType.Unknown:
                    return "U";
                default:
                    throw new InvalidOperationException();
            }
        }

        private char GetLastCharacterStringBuilder(StringBuilder outputSb)
        {
            var length = outputSb.Length;
            if (length == 0)
                return char.MinValue;

            return outputSb[length - 1];
        }

        private IPlayer GetPlayerX(string formatItem)
        {
            var playerNumber = int.Parse(formatItem.Substring(formatItem.IndexOf('/') + 1), NumberStyles.None);
            return _replay.Players.Where(p => p.Identifier == playerNumber).FirstOrDefault();
        }

        private int TakeWhileWhiteSpaceCount(char[] formatItemChars, int startIndex)
        {
            var maxIndex = formatItemChars.Count() - 1;
            if (startIndex > maxIndex)
                return 0;

            int count = startIndex;
            while (count <= maxIndex && char.IsWhiteSpace(formatItemChars[count++])) ;
            return Math.Max(0, count - startIndex - 1);
        }

        #endregion

        //TODO support custom separator?
        #region syntax item methods

        private string GetWinningRaces()
        {
            var winners = _replay.Winners;
            return winners == null ? "NoWinners" : string.Join(",", winners.Select(w => w.RaceType));
        }

        private string GetLosingRaces()
        {
            var losers = _replay.Players.Except(_replay.Winners);
            return losers == null ? "NoLosers" : string.Join(",", losers.Select(l => l.RaceType));
        }

        private string GetRaces()
        {
            var players = _replay.Players;
            return players == null ? "NoPlayers" : string.Join(",", players.Select(p => p.RaceType));
        }

        private string GetWinningTeam()
        {
            //TODO: Issue with players of different ForceIdentifiers both winning...
            var winningTeam = _replay.Winners;
            return winningTeam == null ? "NoWinners" : string.Join(",", winningTeam);
        }

        private string GetLosingTeam()
        {
            var losingTeam = _replay.Players.Except(_replay.Winners);
            return losingTeam == null ? "NoLosers" : string.Join(",", losingTeam);
        }

        private string GetTeams()
        {
            var outputSb = new StringBuilder();
            var playersMinusObservers = _replay.Players.Except(_replay.Observers);
            var playersPerTeam = playersMinusObservers.GroupBy(p => p.ForceIdentifier);
            foreach (var team in playersPerTeam)
            {
                outputSb.Append("(");
                outputSb.Append(string.Join(",", team));
                outputSb.Append(")");
            }
            return outputSb.ToString();
        }

        private string GetMap(OutputFormat outputFormat)
        {
            var map = _replay.ReplayMap.MapName;
            return outputFormat == OutputFormat.Short ? new string(map.Split( ).Select(word => word.First()).ToArray()) : map;
        }

        private string GetMatchup()
        {
            var outputSb = new StringBuilder();
            var playersMinusObservers = _replay.Players.Except(_replay.Observers);
            var playersPerTeam = playersMinusObservers.GroupBy(p => p.ForceIdentifier);
            return string.Join("vs", playersPerTeam.Select(team => string.Join(string.Empty, team.Select(p => p.RaceType.ToString().First()).OrderBy(c => c))));
        }

        private string GetDate()
        {
            return _replay.Timestamp.ToString("yy-MM-dd", CultureInfo.InvariantCulture);
        }

        private string GetDateTime()
        {
            return _replay.Timestamp.ToString("yy-MM-dd hh:mm:ss", CultureInfo.InvariantCulture);
        }

        private string GetDuration(OutputFormat outputFormat)
        {
            var durationSb = new StringBuilder();
            var gameLength = TimeSpan.FromSeconds(Math.Round(_replay.FrameCount / Constants.FastestFPS));
            if (outputFormat == OutputFormat.Short)
            {
                durationSb.Append(gameLength.Hours == 0 ? string.Empty : $"{gameLength.Hours.ToString().PadLeft(2, '0')}:");
                durationSb.Append($"{gameLength.Minutes.ToString().PadLeft(2, '0')}:");
                durationSb.Append($"{gameLength.Seconds.ToString().PadLeft(2, '0')}:");
            }
            else
            {
                durationSb.Append(gameLength.Hours == 0 ? string.Empty : $"{gameLength.Hours.ToString()} hours");
                durationSb.Append(gameLength.Minutes == 0 ? string.Empty : $"{gameLength.Minutes.ToString()} minutes");
                durationSb.Append(gameLength.Seconds == 0 ? string.Empty : $"{gameLength.Seconds.ToString()} seconds");
            }
            return durationSb.ToString();
        }

        private string GetGameFormat()
        {
            switch (_replay.GameType)
            {
                case GameType.CaptureTheFlag:
                case GameType.Greed:
                case GameType.Slaughter:
                case GameType.SuddenDeath:
                case GameType.Ladder:
                case GameType.UseMapSettings:
                case GameType.TeamMelee:
                case GameType.TeamFreeForAll:
                case GameType.TeamCaptureTheFlag:
                case GameType.TopVsBottom:
                case GameType.Unknown:
                case GameType.Melee:
                    {
                        var groupedPlayers = _replay.Players?.Except(_replay.Observers).GroupBy(p => p.ForceIdentifier);
                        if (groupedPlayers == null)
                            return "NoPlayers";

                        return string.Join("v", groupedPlayers.Select(group => group.Count()));
                    }

                case GameType.FreeForAll:
                    {
                        var playerCount = _replay.Players?.Except(_replay.Observers).Count() ?? 0;
                        return playerCount == 0 ? "NoPlayers" : string.Join("v", new string('1', playerCount).AsEnumerable());
                    }

                case GameType.OneOnOne:
                    return "1v1";

                default:
                    throw new InvalidOperationException();
            }
        }

        private string GetGameType(OutputFormat outputFormat)
        {
            var gameType = _replay.GameType;
            return outputFormat == OutputFormat.Short ? GetShortFormGameType(gameType) : gameType.ToString();
        }

        private string GetPlayerInfo(string formatItem)
        {
            var players = _replay.Players.Except(_replay.Observers);
            if (players == null)
                return "NoPlayers";

            var playerRace = new Func<IPlayer, string>(p => p.RaceType.ToString());
            var playerVictoryStatus = new Func<IReplay, IPlayer, bool>((r, p) => r.Winners.Contains(p));

            var outputSb = new StringBuilder();
            var formatItemChars = formatItem.ToCharArray();
            var charCount = formatItemChars.Count();
            int toSkip = 0;
            var playerCount = players.Count();
            for (int playerIndex = 0; playerIndex < playerCount; playerIndex++)
            {
                var player = players.ElementAt(playerIndex);
                for (int i = 0; i < charCount; i++)
                {
                    if (toSkip > 0)
                    {
                        toSkip--;
                        continue;
                    }

                    var c = formatItemChars[i];

                    if (c == '/')
                    {
                        var nextCharIndex = i + 1;
                        if (nextCharIndex < charCount)
                        {
                            toSkip += 1 + TakeWhileWhiteSpaceCount(formatItemChars, i + 2);
                            switch (formatItemChars[nextCharIndex])
                            {
                                case 'p':
                                    outputSb.Append(player.Name);
                                    break;
                                case 'r':
                                    outputSb.Append(playerRace(player).First());
                                    break;
                                case 'R':
                                    outputSb.Append(playerRace(player));
                                    break;
                                case 'w':
                                    char toAppend = playerVictoryStatus(_replay, player) ? 'W' : 'L';
                                    char lastChar = GetLastCharacterStringBuilder(outputSb);
                                    outputSb.Append((char.IsWhiteSpace(lastChar) || char.MinValue == lastChar) ? toAppend.ToString() : $" {toAppend}");
                                    break;
                                case 'W':
                                    string toAppendString = playerVictoryStatus(_replay, player) ? "Winner" : "Loser";
                                    char lastCharacter = GetLastCharacterStringBuilder(outputSb);
                                    outputSb.Append((char.IsWhiteSpace(lastCharacter) || char.MinValue == lastCharacter) ? toAppendString : $" {toAppendString}");
                                    break;
                                default:
                                    throw new InvalidOperationException();
                            }
                        }
                    }
                    else
                    {
                        outputSb.Append(c);
                    }
                }
                if (playerIndex < playerCount - 1)
                {
                    outputSb.Append(", ");
                }
            }
            return outputSb.ToString();
        }

        private string GetPlayers(string formatItem = null)
        {
            if (formatItem == null)
            {
                var players = _replay.Players;
                return players == null ? string.Empty : string.Join(",", players);
            }
            else
            {
                var player = GetPlayerX(formatItem);
                if (player == null)
                    return "NoPlayer";

                return player.Name;
            }
        }

        private string GetPlayersRaces(string formatItem, OutputFormat outputFormat)
        {
            var player = GetPlayerX(formatItem);
            if (player == null)
                return "NoPlayer";

            return outputFormat == OutputFormat.Short ? player.RaceType.ToString().First().ToString() : player.RaceType.ToString();
        }

        private string GetPlayersVictoryStatus(string formatItem, OutputFormat outputFormat)
        {
            var player = GetPlayerX(formatItem);
            if (player == null)
                return "NoPlayer";

            var victoryStatusString = _replay.Winners.Contains(player) ? "Winner" : "Loser";
            return outputFormat == OutputFormat.Short ? victoryStatusString.First().ToString() : victoryStatusString;
        }

        #endregion

        #region constructor

        private ReplayDecorator(IReplay replay)
        {
            _replay = replay;
        }

        #endregion

        #endregion

        #endregion

        #region public

        #region constructor

        public static ReplayDecorator Create(IReplay replay)
        {
            if (replay == null) throw new NullReferenceException(nameof(replay));
            return new ReplayDecorator(replay);
        }

        #endregion

        #region methods

        public string GetReplayItem(Tuple<CustomReplayNameSyntax, string> customReplayNameSyntaxItem)
        {
            var customReplayNameSyntax = customReplayNameSyntaxItem.Item1;
            switch (customReplayNameSyntax)
            {
                case CustomReplayNameSyntax.WinningRaces:
                    return GetWinningRaces();

                case CustomReplayNameSyntax.LosingRaces:
                    return GetLosingRaces();

                case CustomReplayNameSyntax.Races:
                    return GetRaces();

                case CustomReplayNameSyntax.WinningTeams:
                    return GetWinningTeam();

                case CustomReplayNameSyntax.LosingTeams:
                    return GetLosingTeam();

                case CustomReplayNameSyntax.Teams:
                    return GetTeams();

                case CustomReplayNameSyntax.MapShort:
                    return GetMap(OutputFormat.Short);

                case CustomReplayNameSyntax.MapLong:
                    return GetMap(OutputFormat.Long);

                case CustomReplayNameSyntax.Matchup:
                    return GetMatchup();

                case CustomReplayNameSyntax.Date:
                    return GetDate();

                case CustomReplayNameSyntax.DateTime:
                    return GetDateTime();

                case CustomReplayNameSyntax.DurationShort:
                    return GetDuration(OutputFormat.Short);

                case CustomReplayNameSyntax.DurationLong:
                    return GetDuration(OutputFormat.Long);

                case CustomReplayNameSyntax.GameFormat:
                    return GetGameFormat();

                case CustomReplayNameSyntax.GameTypeShort:
                    return GetGameType(OutputFormat.Short);

                case CustomReplayNameSyntax.GameTypeLong:
                    return GetGameType(OutputFormat.Long);

                case CustomReplayNameSyntax.PlayerInfo:
                    return GetPlayerInfo(customReplayNameSyntaxItem.Item2);

                case CustomReplayNameSyntax.Players:
                    return GetPlayers();

                case CustomReplayNameSyntax.PlayerX:
                    return GetPlayers(customReplayNameSyntaxItem.Item2);

                case CustomReplayNameSyntax.PlayerXRaceShort:
                    return GetPlayersRaces(customReplayNameSyntaxItem.Item2, OutputFormat.Short);

                case CustomReplayNameSyntax.PlayerXRaceLong:
                    return GetPlayersRaces(customReplayNameSyntaxItem.Item2, OutputFormat.Long);

                case CustomReplayNameSyntax.PlayerXVictoryStatusShort:
                    return GetPlayersVictoryStatus(customReplayNameSyntaxItem.Item2, OutputFormat.Long);

                case CustomReplayNameSyntax.PlayerXVictoryStatusLong:
                    return GetPlayersVictoryStatus(customReplayNameSyntaxItem.Item2, OutputFormat.Long);

                default:
                    throw new InvalidOperationException();
            }
        }

        #endregion

        #endregion
    }
}
