using ReplayParser.Entities;
using ReplayParser.Interfaces;
using System.Globalization;
using System.Linq;
using System.Text;
using System;
using ReplayParser.ReplaySorter.IO;
using System.Text.RegularExpressions;
using System.IO;
using ReplayParser.ReplaySorter.Extensions;
using System.Collections.Generic;

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

        private readonly File<IReplay> _replayFile;
        private readonly IReplay _replay;
        private static readonly Regex _ignoreMapNameParts = new Regex(@"iccup|\d+|\d+\.\d+", RegexOptions.IgnoreCase);
        private static readonly Regex _digit = new Regex(@"\d");

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

        private IPlayer GetPlayerX(string formatItem)
        {
            var firstDigitMatch = _digit.Match(formatItem);
            if (!firstDigitMatch.Success)
                throw new InvalidOperationException($"{nameof(formatItem)} does not contain a valid player identifier!");

            var playerNumber = int.Parse(formatItem.Substring(firstDigitMatch.Index), NumberStyles.None);
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
            return winningTeam == null ? "NoWinners" : string.Join(",", winningTeam.Select(p => p.Name));
        }

        private string GetLosingTeam()
        {
            var losingTeam = _replay.Players.Except(_replay.Winners);
            return losingTeam == null ? "NoLosers" : string.Join(",", losingTeam.Select(p => p.Name));
        }

        private string GetTeams()
        {
            var outputSb = new StringBuilder();
            var playersMinusObservers = _replay.Players.Except(_replay.Observers);
            var playersPerTeam = playersMinusObservers.GroupBy(p => p.ForceIdentifier);
            foreach (var team in playersPerTeam)
            {
                outputSb.Append("(");
                outputSb.Append(string.Join(",", team.Select(p => p.Name)));
                outputSb.Append(")");
            }
            return outputSb.ToString();
        }

        private string GetMap(OutputFormat outputFormat)
        {
            var map = _replay.ReplayMap.MapName;
            map = FileHandler.RemoveInvalidChars(map);
            return outputFormat == OutputFormat.Short ? new string(map.Split((char[])null, StringSplitOptions.RemoveEmptyEntries).Where(word => !_ignoreMapNameParts.IsMatch(word)).Select(word => word.First()).ToArray()) : map;
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
            return _replay.Timestamp.ToString("yy-MM-ddThhmmss", CultureInfo.InvariantCulture);
        }

        private string GetDuration(OutputFormat outputFormat)
        {
            var durationSb = new StringBuilder();
            var gameLength = TimeSpan.FromSeconds(Math.Round(_replay.FrameCount / Constants.FastestFPS));
            if (outputFormat == OutputFormat.Short)
            {
                durationSb.Append(gameLength.Hours == 0 ? string.Empty : $"{gameLength.Hours.ToString().PadLeft(2, '0')}_");
                durationSb.Append($"{gameLength.Minutes.ToString().PadLeft(2, '0')}_");
                durationSb.Append($"{gameLength.Seconds.ToString().PadLeft(2, '0')}");
            }
            else
            {
                durationSb.Append(gameLength.Hours == 0 ? string.Empty : $"{gameLength.Hours.ToString()} hours");
                durationSb.TryAddSingleSpace();
                durationSb.Append(gameLength.Minutes == 0 ? string.Empty : $"{gameLength.Minutes.ToString()} minutes");
                durationSb.TryAddSingleSpace();
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
                        var groupedPlayers = _replay.Players?.Except(_replay.Observers ?? Enumerable.Empty<IPlayer>()).GroupBy(p => p.ForceIdentifier);
                        if (groupedPlayers == null)
                            return "NoPlayers";

                        return string.Join("v", groupedPlayers.Select(group => group.Count()));
                    }

                case GameType.FreeForAll:
                    {
                        var playerCount = _replay.Players?.Except(_replay.Observers ?? Enumerable.Empty<IPlayer>()).Count() ?? 0;
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
            var players = _replay.Players.Except(_replay.Observers).OrderBy(p => p.ForceIdentifier);
            if (players == null || players.Count() == 0)
                return "NoPlayers";

            var playerRace = new Func<IPlayer, string>(p => p.RaceType.ToString());
            var playerVictoryStatus = new Func<IReplay, IPlayer, bool>((r, p) => r.Winners.Contains(p));

            var outputSb = new StringBuilder();
            var formatItemChars = formatItem.Trim('>', '<').TrimEnd('/').ToCharArray();
            var charCount = formatItemChars.Count();
            int toSkip = 0;
            var playerCount = players.Count();
            var currentTeam = players.First().ForceIdentifier;
            for (int playerIndex = 0; playerIndex < playerCount; playerIndex++)
            {
                var player = players.ElementAt(playerIndex);
                if (currentTeam != player.ForceIdentifier)
                {
                    currentTeam = player.ForceIdentifier;
                    if (outputSb.Length >= 2)
                    {
                        outputSb.Replace(", ", string.Empty, outputSb.Length - 2, 2);
                    }
                    outputSb.Append(" vs ");
                }
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
                            // toSkip += 1 + TakeWhileWhiteSpaceCount(formatItemChars, i + 2);
                            toSkip += 1;
                            // outputSb.TryAddSingleSpace();

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
                                    outputSb.Append(playerVictoryStatus(_replay, player) ? 'W' : 'L');
                                    break;
                                case 'W':
                                    outputSb.Append(playerVictoryStatus(_replay, player) ? "Winner" : "Loser");
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

        private string GetPlayersWithObservers(string formatItem = null)
        {
            if (formatItem == null)
            {
                var players = _replay.Players;
                return players == null ? "NoPlayers" : string.Join(",", players.Select(p => p.Name));
            }
            else
            {
                var player = GetPlayerX(formatItem);
                if (player == null)
                    return "NoPlayer";

                return player.Name;
            }
        }

        private string GetPlayers()
        {
            var playersWithoutObservers = _replay.Players.Except(_replay.Observers);
            return playersWithoutObservers == null ? "NoPlayers" : string.Join(",", playersWithoutObservers.Select(p => p.Name));
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

        private string GetOriginalName()
        {
            return Path.GetFileNameWithoutExtension(_replayFile.FilePath);
        }

        private string GetCounter(int counter, int maxCounter, OutputFormat outputFormat)
        {
            if (outputFormat == OutputFormat.Short)
                return counter.ToString();

            return counter.ToString().PadLeft(maxCounter.ToString().Length, '0');
        }

        #endregion

        #region constructor

        private ReplayDecorator(File<IReplay> replayFile)
        {
            _replayFile = replayFile;
            _replay = replayFile.Content;
        }

        #endregion

        #endregion

        #endregion

        #region public

        #region constructor

        public static ReplayDecorator Create(File<IReplay> replayFile)
        {
            if (replayFile == null) throw new NullReferenceException(nameof(replayFile));
            return new ReplayDecorator(replayFile);
        }

        #endregion

        #region methods

        public string GetReplayItem(Tuple<CustomReplayNameSyntax, string> customReplayNameSyntaxItem, int counter, int maxCounter)
        {
            var customReplayNameSyntax = customReplayNameSyntaxItem.Item1;
            switch (customReplayNameSyntax)
            {
                case CustomReplayNameSyntax.None:
                    return customReplayNameSyntaxItem.Item2;

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

                case CustomReplayNameSyntax.PlayersWithObservers:
                    return GetPlayersWithObservers();

                case CustomReplayNameSyntax.Players:
                    return GetPlayers();

                case CustomReplayNameSyntax.PlayerX:
                    return GetPlayersWithObservers(customReplayNameSyntaxItem.Item2);

                case CustomReplayNameSyntax.PlayerXRaceShort:
                    return GetPlayersRaces(customReplayNameSyntaxItem.Item2, OutputFormat.Short);

                case CustomReplayNameSyntax.PlayerXRaceLong:
                    return GetPlayersRaces(customReplayNameSyntaxItem.Item2, OutputFormat.Long);

                case CustomReplayNameSyntax.PlayerXVictoryStatusShort:
                    return GetPlayersVictoryStatus(customReplayNameSyntaxItem.Item2, OutputFormat.Short);

                case CustomReplayNameSyntax.PlayerXVictoryStatusLong:
                    return GetPlayersVictoryStatus(customReplayNameSyntaxItem.Item2, OutputFormat.Long);

                case CustomReplayNameSyntax.OriginalName:
                    return GetOriginalName();

                case CustomReplayNameSyntax.CounterShort:
                    return GetCounter(counter, maxCounter, OutputFormat.Short);

                case CustomReplayNameSyntax.CounterLong:
                    return GetCounter(counter, maxCounter, OutputFormat.Long);

                default:
                    throw new InvalidOperationException();
            }
        }

        public string GameFormat()
        {
            return GetGameFormat();
        }

        public string Matchup()
        {
            return GetMatchup();
        }

        #endregion

        #endregion
    }
}
