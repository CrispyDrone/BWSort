using ReplayParser.Interfaces;
using ReplayParser.ReplaySorter.IO;
using ReplayParser.ReplaySorter.ReplayRenamer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ReplayParser.ReplaySorter.Filtering
{
    public class ReplayFilterer
    {
        private const int MAPCODE = 1;
        private const int DURATIONCODE = 2;
        private const int MATCHUPCODE = 3;
        private const int PLAYERCODE = 4;
        private const int DATECODE = 5;

        private static readonly Regex MAPFILTERLABEL = new Regex("m:");
        private static readonly Regex DURATIONFILTERLABEL = new Regex("du:");
        private static readonly Regex MATCHUPFILTERLABEL = new Regex("mu:");
        private static readonly Regex PLAYERFILTERLABEL = new Regex("p:");
        private static readonly Regex DATEFILTERLABEL = new Regex("d:");

        private readonly Dictionary<Regex, int> _labelToCodeMap = new Dictionary<Regex, int>()
        {
            { MAPFILTERLABEL, MAPCODE },
            { DURATIONFILTERLABEL, DURATIONCODE },
            { MATCHUPFILTERLABEL, MATCHUPCODE },
            { PLAYERFILTERLABEL, PLAYERCODE },
            { DATEFILTERLABEL, DATECODE }
        };

        private readonly Regex FILTERLABEL = new Regex("([md]u?|[pd]):");

        public List<File<IReplay>> Apply(List<File<IReplay>> list, string filterExpression)
        {
            Dictionary<int, string> filterExpressions = ExtractFilters(filterExpression);
            if (filterExpressions == null)
                return list;

            Func<File<IReplay>, bool>[] queries = ParseFilters(filterExpressions);
            if (queries == null || queries.Count() == 0)
                return list;

            return ApplyTo(list, queries);
        }

        private Dictionary<int, string> ExtractFilters(string filterExpression)
        {
            var matches = FILTERLABEL.Matches(filterExpression);

            if (matches.Count == 0)
                return null;

            Dictionary<int, string> filters = new Dictionary<int, string>();

            foreach (Match match in matches)
            {
                if (!ValidateMatch(match, filterExpression))
                    return null;

                int? code = MapMatch(match);
                if (!code.HasValue)
                    return null;

                if (filters.ContainsKey(code.Value))
                    return null;

                filters[code.Value] = GetFilter(match, filterExpression);
            }
            return filters;
        }

        private string GetFilter(Match match, string filterExpression)
        {
            // Remove the filterlabel + extract until next match if there is any otherwise till end of string
            var nextMatch = match.NextMatch();

            int nextMatchIndex = -1;

            if (nextMatch.Success)
                nextMatchIndex = nextMatch.Index;

            int start = match.Index + match.Value.Length;

            return nextMatchIndex == -1 ? filterExpression.Substring(start) : filterExpression.Substring(start, nextMatchIndex - start);
        }

        private bool ValidateMatch(Match match, string filterExpression)
        {
            return AtBeginning(match, filterExpression) || FollowingComma(match, filterExpression);
        }

        private bool AtBeginning(Match match, string filterExpression)
        {
            if (match.Index == 0)
                return true;

            string beforeMatch = filterExpression.Substring(0, match.Index);
            return string.IsNullOrWhiteSpace(beforeMatch);
        }

        private bool FollowingComma(Match match, string filterExpression)
        {
            if (match.Index == 0)
                return false;

            var beforeMatch = filterExpression.Substring(0, match.Index);
            var lastComma = beforeMatch.LastIndexOf(',');
            if (lastComma <= 0)
                return false;

            var betweenCommaAndMatch = filterExpression.Substring(lastComma + 1, beforeMatch.Length - lastComma - 1);
            return string.IsNullOrWhiteSpace(betweenCommaAndMatch);
        }

        private int? MapMatch(Match match)
        {
            foreach (var filter in _labelToCodeMap.Keys)
            {
                if (filter.IsMatch(match.Value))
                    return _labelToCodeMap[filter];
            }

            return null;
        }

        private Func<File<IReplay>, bool>[] ParseFilters(Dictionary<int, string> filterExpressions)
        {
            Func<File<IReplay>, bool>[] funcs = new Func<File<IReplay>, bool>[filterExpressions.Count];
            int counter = 0;

            foreach (var filterExpression in filterExpressions)
            {
                switch (filterExpression.Key)
                {
                    case MAPCODE:
                        funcs[counter] = ParseMapFilter(filterExpression.Value);
                        break;
                    case DURATIONCODE:
                        funcs[counter] = ParseDurationFilter(filterExpression.Value);
                        break;
                    case MATCHUPCODE:
                        funcs[counter] = ParseMatchupFilter(filterExpression.Value);
                        break;
                    case PLAYERCODE:
                        funcs[counter] = ParsePlayerFilter(filterExpression.Value);
                        break;
                    case DATECODE:
                        funcs[counter] = ParseDateFilter(filterExpression.Value);
                        break;
                    default:
                        return null;
                }
                counter++;
            }
            return funcs;
        }
        private List<File<IReplay>> ApplyTo(List<File<IReplay>> list, Func<File<IReplay>, bool>[] queries)
        {
            IQueryable<File<IReplay>> filteredList = new List<File<IReplay>>(list).AsQueryable();

            for (int i = 0; i < queries.Length; i++)
            {
                int index = i;
                filteredList = filteredList.Where(r => queries[index](r));
            }
            return filteredList.ToList();
        }

        private Func<File<IReplay>, bool> ParseDateFilter(string value)
        {
            throw new NotImplementedException();
        }

        private Func<File<IReplay>, bool> ParsePlayerFilter(string value)
        {
            throw new NotImplementedException();
        }

        private Func<File<IReplay>, bool> ParseMatchupFilter(string matchupExpressionString)
        {
            if (string.IsNullOrWhiteSpace(matchupExpressionString))
                return null;

            matchupExpressionString = matchupExpressionString.TrimEnd(',');

            var matchupExpressions = matchupExpressionString.Split(new char[] { '|' });

            if (matchupExpressions.Count() == 0)
                return null;

            Expression<Func<File<IReplay>, bool>> filterExpression = null;
            var replay = Expression.Parameter(typeof(File<IReplay>), "r");
            var replayProper = Expression.PropertyOrField(replay, "Content");

            foreach (var matchupExpression in matchupExpressions)
            {
                // var replayTeams = Expression.New(typeof(Teams).GetConstructor(new Type[] { typeof(IReplay) }));
                var replayTeams = Expression.New(typeof(Teams).GetConstructor(new Type[] { typeof(IReplay) }), replayProper);
                var replayMatchup = Expression.New(typeof(MatchUp).GetConstructor(new Type[] { typeof(IReplay), typeof(Teams) }), replayProper, replayTeams);
                var replayMatchupAsString = Expression.Call(replayMatchup, typeof(MatchUp).GetMethod("GetSection"), Expression.Constant(string.Empty, typeof(string)));
                // where(r => CompareMatchups(new matchup(new team(replay), replay).GetSection(), matchupstring))
                Expression body = Expression.IsTrue(
                        Expression.Call(
                            typeof(ReplayFilterer).GetMethod(nameof(ReplayFilterer.CompareMatchups), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static), 
                            replayMatchupAsString, 
                            Expression.Constant(matchupExpression, typeof(string))
                        )
                    );

                filterExpression = CreateOrAddOrExpression(filterExpression, body, replay);
            }

            return filterExpression.Compile();
        }

        private static bool CompareMatchups(string matchupA, string matchupExpression)
        {
            // z,p,t,r
            Dictionary<int, int[]> raceCountsA = GetRaceCountsPerTeam(matchupA, false);
            // z,p,t,r,'.'
            Dictionary<int[], int> raceCountsB = GetTeamPerRaceCountsAndPlaceHolder(matchupExpression);

            if (raceCountsA == null || raceCountsB == null)
                return false;

            if (raceCountsA.Keys.Count != raceCountsB.Values.Count)
                return false;

            bool[] teamsMatched = new bool[raceCountsB.Values.Count];

            foreach (var team in raceCountsA.Keys)
            {
                if (!raceCountsB.ContainsKey(raceCountsA[team]))
                {
                    return false;
                }
                else
                {
                    if (teamsMatched[raceCountsB[raceCountsA[team]]])
                    {
                        return false;
                    }
                    else
                    {
                        teamsMatched[raceCountsB[raceCountsA[team]]] = true;
                    }
                }
            }

            return true;
        }

        private static readonly string _versusPattern = "vs?";
        private static readonly Regex _versus = new Regex(_versusPattern);

        private static Dictionary<int[], int> GetTeamPerRaceCountsAndPlaceHolder(string matchupExpression)
        {
            return GetRaceCountsPerTeam(matchupExpression, true)?.ToDictionary(kvp => kvp.Value, kvp => kvp.Key, new RaceEqWithWildCardComparer());
        }

        private static Dictionary<int, int[]> GetRaceCountsPerTeam(string matchupA, bool countPlaceHolders = false)
        {
            var matchupStrings = GetMatchups(matchupA);

            if (matchupStrings == null)
                return null;

            Dictionary<int, int[]> raceCountsPerTeam = new Dictionary<int, int[]>();
            int teamNumber = 0;

            foreach (var team in matchupStrings)
            {
                raceCountsPerTeam[teamNumber] = GetRaceCounts(team, countPlaceHolders);
                teamNumber++;
            }

            return raceCountsPerTeam;
        }

        //TODO use stringbuilder??
        private static string[] GetMatchups(string matchupA)
        {
            var matches = _versus.Matches(matchupA);

            if (matches.Count == 0)
                return null;

            int counter = 0;
            string[] matchups = new string[matches.Count + 1];

            foreach (Match match in matches)
            {
                matchups[counter] = matchupA.Substring(0, match.Index);
                matchupA.Remove(0, match.Index + match.Value.Length);
                counter++;
            }

            if (string.IsNullOrWhiteSpace(matchupA))
                return null;

            matchups[counter] = matchupA;

            return matchups;
        }

        private static int[] GetRaceCounts(string matchupA, bool countPlaceHolders = false)
        {
            int[] raceCounts = countPlaceHolders ? new int[5] : new int[4];

            for (int i = 0; i < matchupA.Length; i++)
            {
                switch(matchupA[i])
                {
                    case 'z':
                    case 'Z':
                        raceCounts[0]++;
                        break;
                    case 'p':
                    case 'P':
                        raceCounts[1]++;
                        break;
                    case 't':
                    case 'T':
                        raceCounts[2]++;
                        break;
                    case 'r':
                    case 'R':
                        raceCounts[3]++;
                        break;
                    case '.':
                        if (countPlaceHolders)
                        {
                            raceCounts[4]++;
                        }
                        break;
                    default:
                        return countPlaceHolders ? new int[5] : new int[4];
                }
            }
            return raceCounts;
        }

        // number - timeunit [ - number - timeunit ]1-2
        // classical 05:10, 00:20:17
        private static readonly string _lessThanGreaterThanOperatorsPattern = "^(<|<=|>|>=)?";
        private static readonly string _digitalMinutesSecondsPattern = "^(\\d{2}):(\\d{2})$";
        private static readonly string _digitalHoursMinutesSecondsPattern = "^(\\d{2}):(\\d{2}):(\\d{2})$";
        private static readonly string _writtenHoursMinutesSecondsPattern = "^(?:(\\d+)(?:h(?:rs|hours)?))?(?:(\\d+)(?:m(?:in(?:utes)?)?))?(?:(\\d+)(?:s(?:ec(?:onds)?)?))?";
        private static readonly string _timeRangePattern = "^between\\s*(.*?)-(.*)$";
        private static readonly Regex _lessThanGreaterThanOperatorsRegex = new Regex(_lessThanGreaterThanOperatorsPattern);
        private static readonly Regex _digitalMinutesSecondsRegex = new Regex(_digitalMinutesSecondsPattern);
        private static readonly Regex _digitalHoursMinutesSecondsRegex = new Regex(_digitalHoursMinutesSecondsPattern);
        private static readonly Regex _writtenHoursMinutesSecondsRegex = new Regex(_writtenHoursMinutesSecondsPattern);
        private static readonly Regex _timeRangeRegex = new Regex(_timeRangePattern);

        private Func<File<IReplay>, bool> ParseDurationFilter(string durationExpressionString)
        {
            var durationExpressions = durationExpressionString.Split(new char[] { '|' });

            if (durationExpressions.Count() == 0)
                return null;

            Expression<Func<File<IReplay>, bool>> filterExpression = null;
            var replay = Expression.Parameter(typeof(File<IReplay>), "r");

            foreach (var durationExpression in durationExpressions)
            {
                int?[] durationsAsFrameCounts = ParseDuration(durationExpression);

                if (durationsAsFrameCounts == null || durationsAsFrameCounts.Count() == 0 || durationsAsFrameCounts.Any(f => f == null))
                    continue;

                Expression replayDuration = Expression.PropertyOrField(Expression.PropertyOrField(replay, "Content"), "FrameCount");

                Expression body = null;

                if (durationsAsFrameCounts.Count() > 1)
                {
                    // between, inclusive...
                    body = Expression.AndAlso(
                        Expression.IsTrue(
                                Expression.GreaterThanOrEqual(
                                    replayDuration, Expression.Constant(durationsAsFrameCounts[0])
                            )
                        ),
                        Expression.IsTrue(
                            Expression.LessThanOrEqual(
                                replayDuration, Expression.Constant(durationsAsFrameCounts[1])
                                )
                        )
                    );
                }
                else
                {
                    int? comparison = ParseComparison(durationExpression);
                    if (comparison == null)
                        continue;

                    // <= : -2, < : -1, = : 0, > : 1, >= 2
                    var replayFilter = Expression.Constant(durationsAsFrameCounts[0]);
                    //TODO extract to function
                    switch (comparison)
                    {
                        case -2:
                            body = Expression.LessThanOrEqual(replayDuration, replayFilter);
                            break;
                        case -1:
                            body = Expression.LessThan(replayDuration, replayFilter);
                            break;
                        case 0:
                            body = Expression.Equal(replayDuration, replayFilter);
                            break;
                        case 1:
                            body = Expression.GreaterThan(replayDuration, replayFilter);
                            break;
                        case 2:
                            body = Expression.GreaterThanOrEqual(replayDuration, replayFilter);
                            break;
                        default:
                            throw new Exception();
                    }
                }

                filterExpression = CreateOrAddOrExpression(filterExpression, body, replay);
            }
            return filterExpression.Compile();
        }

        //TODO extract to constants class or something
        private const double FastestFPS = (double)1000 / 42;

        private int?[] ParseDuration(string durationExpressionString)
        {
            if (string.IsNullOrWhiteSpace(durationExpressionString)) return null;

            if (_timeRangeRegex.IsMatch(durationExpressionString))
            {
                var matches = _timeRangeRegex.Matches(durationExpressionString);
                if (matches.Count == 0 || matches.Count > 1)
                    return null;

                int?[] durationExpressionsAsFrameCount = new int?[2];

                for (int i = 0; i < 2; i++)
                {
                    var timeValue = matches[0].Groups[i + 1];
                    durationExpressionsAsFrameCount[i] = ParseTimevalue(timeValue.Value);
                }

                return durationExpressionsAsFrameCount;
            }
            else
            {
                return new int?[1] { ParseTimevalue(durationExpressionString) };
            }
        }

        private int? ParseTimevalue(string timeValue)
        {
            if (string.IsNullOrWhiteSpace(timeValue))
                return null;

            //TODO extract common logic
            timeValue = RemoveComparisonOperator(timeValue);
            Match match = null;

            if (_digitalHoursMinutesSecondsRegex.IsMatch(timeValue))
            {
                match = _digitalHoursMinutesSecondsRegex.Match(timeValue);
            }
            else if (_digitalMinutesSecondsRegex.IsMatch(timeValue))
            {
                match = _digitalMinutesSecondsRegex.Match(timeValue);
            }
            else if (_writtenHoursMinutesSecondsRegex.IsMatch(timeValue))
            {
                //TODO make helper method "EnsureSingleMatch" or something
                match = _writtenHoursMinutesSecondsRegex.Match(timeValue);
            }
            else
            {
                // reevaluate whether you should be throwing exceptions... this is not an exceptional state. See if you can return nulls instead
                return null;
            }

            // hours, minutes, seconds
            int[] timeParts = new int[3];
            for (int i = match.Groups.Count - 1, j = 2; i > 0; i--, j--)
            {
                int.TryParse(match.Groups[i].Value, out timeParts[j]);
            }

            //TODO make helper function to calculate frames from time value
            return (int)((timeParts[0] * 3600 + timeParts[1] * 60 + timeParts[2]) * FastestFPS);
        }

        //TODO extract common logic
        private string RemoveComparisonOperator(string timeValue)
        {
            if (!_lessThanGreaterThanOperatorsRegex.IsMatch(timeValue))
                return null;

            var match = _lessThanGreaterThanOperatorsRegex.Match(timeValue);

            if (match.Index > 0)
                return null;

            return timeValue.Remove(0, match.Value.Length);
        }

        //TODO extract common logic
        private int? ParseComparison(string timeValue)
        {
            if (!_lessThanGreaterThanOperatorsRegex.IsMatch(timeValue))
                return null;

            var match = _lessThanGreaterThanOperatorsRegex.Match(timeValue);

            if (match.Index > 0)
                return null;

            string comparison = match.Captures[0].Value;

            switch (comparison)
            {
                case "<=":
                    return -2;
                case "<":
                    return -1;
                case "=":
                    return 0;
                case ">":
                    return 1;
                case ">=":
                    return 2;
                default:
                    return null;
            }
        }

        private Func<File<IReplay>, bool> ParseMapFilter(string mapExpression)
        {
            if (string.IsNullOrWhiteSpace(mapExpression))
                return null;

            Expression<Func<File<IReplay>, bool>> filterExpression = null;
            var maps = mapExpression.Split(new char[] { '|' });
            var replay = Expression.Parameter(typeof(File<IReplay>), "r");

            foreach (var map in maps)
            {
                if (string.IsNullOrWhiteSpace(map)) continue;

                string escapedMapName = EscapeExceptValidWildcards(map);
                var mapRegex = new Regex(escapedMapName);

                // where(r => MapRegex.IsMatch(r.ReplayMap.MapName))
                string mapNameProperty = "ReplayMap.MapName";
                Expression mapNameExpression = Expression.PropertyOrField(replay, "Content");

                foreach (var property in mapNameProperty.Split('.'))
                {
                    mapNameExpression = Expression.PropertyOrField(mapNameExpression, property);
                }

                var body = Expression.IsTrue(
                    Expression.Call(
                        Expression.Constant(mapRegex),
                        typeof(Regex).GetMethod("IsMatch", new Type[] { typeof(string)}),
                        mapNameExpression
                    )
                );

                filterExpression = CreateOrAddOrExpression(filterExpression, body, replay);
            }

            return filterExpression.Compile();
        }

        private Expression<Func<T1, T2>> CreateOrAddOrExpression<T1, T2>(Expression<Func<T1, T2>> expression, Expression body, params ParameterExpression[] parameters)
        {
            if (expression == null)
                return Expression.Lambda<Func<T1, T2>>(body, parameters);

            return Expression.Lambda<Func<T1, T2>>(Expression.Or(expression, Expression.Lambda<Func<T1, T2>>(body, parameters)));
        }


        private string EscapeExceptValidWildcards(string searchString)
        {
            if (string.IsNullOrWhiteSpace(searchString)) return null;

            char firstChar = searchString[0];
            char lastChar = searchString[searchString.Length - 1];
            string middlePartSearchString = searchString.Substring(1, searchString.Length - 2);
            return firstChar == '*' ?
                (
                    lastChar == '*' ?
                        '.' + firstChar + Regex.Escape(middlePartSearchString) + '.' + lastChar :
                        '.' + firstChar + Regex.Escape(middlePartSearchString + lastChar)
                ) :
                (
                    lastChar == '*' ?
                        Regex.Escape(firstChar + middlePartSearchString) + '.' + lastChar :
                        Regex.Escape(firstChar + middlePartSearchString + lastChar)
                );
        }

        private class RaceEqWithWildCardComparer : IEqualityComparer<int[]>
        {
            public bool Equals(int[] raceCountsWithoutWildCards, int[] raceCountsWithWildCards)
            {
                if (raceCountsWithoutWildCards == null || raceCountsWithWildCards == null)
                    return false;

                if (raceCountsWithoutWildCards.Length < 4 || raceCountsWithoutWildCards.Length > 5 || raceCountsWithWildCards.Length < 4 || raceCountsWithWildCards.Length > 5 || raceCountsWithoutWildCards.Length == raceCountsWithWildCards.Length)
                    return false;

                if (raceCountsWithoutWildCards.Length == 5 && raceCountsWithWildCards.Length == 4)
                    return Equals(raceCountsWithWildCards, raceCountsWithoutWildCards);

                int numberOfWildCards = raceCountsWithWildCards[4];
                for (int i = 0; i < raceCountsWithoutWildCards.Length; i++)
                {
                    if (raceCountsWithoutWildCards[i] != raceCountsWithWildCards[i] && --numberOfWildCards < 0)
                        return false;
                }

                return true;
            }

            public int GetHashCode(int[] obj)
            {
                return obj.Sum(x => x).GetHashCode();
            }
        }
    }
}
