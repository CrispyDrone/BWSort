using ReplayParser.Interfaces;
using ReplayParser.ReplaySorter.IO;
using ReplayParser.ReplaySorter.ReplayRenamer;
using ReplayParser.ReplaySorter.Sorting;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;

namespace ReplayParser.ReplaySorter.Filtering
{
    public class ReplayFilterer
    {
        private const int MAPCODE = 1;
        private const int DURATIONCODE = 2;
        private const int MATCHUPCODE = 3;
        private const int PLAYERCODE = 4;
        private const int DATECODE = 5;

        private static readonly Regex MAPFILTERLABEL = new Regex("m:", RegexOptions.IgnoreCase);
        private static readonly Regex DURATIONFILTERLABEL = new Regex("du:", RegexOptions.IgnoreCase);
        private static readonly Regex MATCHUPFILTERLABEL = new Regex("mu:", RegexOptions.IgnoreCase);
        private static readonly Regex PLAYERFILTERLABEL = new Regex("p:", RegexOptions.IgnoreCase);
        private static readonly Regex DATEFILTERLABEL = new Regex("d:", RegexOptions.IgnoreCase);

        private readonly Dictionary<Regex, int> _labelToCodeMap = new Dictionary<Regex, int>()
        {
            { MAPFILTERLABEL, MAPCODE },
            { DURATIONFILTERLABEL, DURATIONCODE },
            { MATCHUPFILTERLABEL, MATCHUPCODE },
            { PLAYERFILTERLABEL, PLAYERCODE },
            { DATEFILTERLABEL, DATECODE }
        };

        private readonly Regex FILTERLABEL = new Regex("([md]u?|[pd]):");

        public List<File<IReplay>> Apply(List<File<IReplay>> list, string filterExpression, BackgroundWorker worker_ReplayFilterer)
        {
            Dictionary<int, string> filterExpressions = ExtractFilters(filterExpression);
            if (filterExpressions == null)
                return list;

            Func<File<IReplay>, bool>[] queries = ParseFilters(filterExpressions);
            if (queries == null || queries.Count() == 0)
                return list;

            return ApplyTo(list, queries, worker_ReplayFilterer);
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

            return nextMatchIndex == -1 ? filterExpression.Substring(start) : filterExpression.Substring(start, nextMatchIndex - (start + 1));
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
        private List<File<IReplay>> ApplyTo(List<File<IReplay>> list, Func<File<IReplay>, bool>[] queries, BackgroundWorker worker_ReplayFilterer)
        {
            IQueryable<File<IReplay>> filteredList = new List<File<IReplay>>(list).AsQueryable();

            var totalNumberOfReplays = list.Count;
            filteredList = filteredList.Where((r, i) => ReportProgress(i + 1, totalNumberOfReplays, worker_ReplayFilterer));
            for (int i = 0; i < queries.Length; i++)
            {
                int index = i;
                if (queries[index] == null) return null;
                filteredList = filteredList.Where(r => queries[index](r));
            }

            return filteredList.ToList();
        }

        private bool ReportProgress(int count, int totalCount, BackgroundWorker worker_ReplayFilterer)
        {
            worker_ReplayFilterer.ReportProgress(Convert.ToInt32(((double)count / totalCount) * 100));
            return true;
        }

        //TODO your regexes should not contain optional spaces at start or end (will give better performance i think...), you should instead trim the user input...
        private const string _dateSeparator = "[-\\.\\/]";
        private static readonly string _digitalYearAndMonthAndDayPattern = $"(\\d{{2,4}})(?:{_dateSeparator}(\\d{{1,2}}))?(?:{_dateSeparator}(\\d{{1,2}}))?";
        private static readonly string _writtenDateWithoutQuantifierPattern = "^(this year|last year|this month|last month|this week|last week|today|yesterday|january|february|march|april|may|june|july|august|september|october|november|december)$";
        private static readonly string _writtenAgoDateWithQuantifierPattern = "(?:(?<WithAgo>(\\d+)\\s+(years|months|weeks|days))\\s+ago)";
        private static readonly string _writtenAgoWithoutAgoDateWithQuantifierPattern = "(?:(?:(\\d+)\\s+(years|months|weeks|days)))";
        private static readonly string _writtenPreviousDateWithQuantifierPattern = "(?:previous\\s+(?:(\\d+)\\s+(years|months|weeks|days)))";
        private static readonly string _writtenCombinableDatePattern = $"(?(^{_writtenAgoWithoutAgoDateWithQuantifierPattern}(?!\\s+ago))(?:(?<FirstWithoutAgo>^{_writtenAgoWithoutAgoDateWithQuantifierPattern})(?: and (?<VariableWithoutAgo>{_writtenAgoWithoutAgoDateWithQuantifierPattern}))* and {_writtenAgoDateWithQuantifierPattern}$)|(?:^{_writtenAgoDateWithQuantifierPattern}$))";
        private static readonly Regex _digitalYearAndMonthAndDayRegex = new Regex(_digitalYearAndMonthAndDayPattern);
        private static readonly Regex _writtenPreviousDateWithQuantifierRegex = new Regex(_writtenPreviousDateWithQuantifierPattern, RegexOptions.IgnoreCase);
        private static readonly Regex _writtenDateWithoutQuantifierRegex = new Regex(_writtenDateWithoutQuantifierPattern, RegexOptions.IgnoreCase);
        private static readonly Regex _writtenAgoDateWithQuantifierRegex = new Regex($"^{_writtenAgoDateWithQuantifierPattern}$", RegexOptions.IgnoreCase);
        private static readonly Regex _writtenCombinableDateRegex = new Regex(_writtenCombinableDatePattern, RegexOptions.IgnoreCase);

        private Func<File<IReplay>, bool> ParseDateFilter(string dateExpressionString)
        {
            if (string.IsNullOrWhiteSpace(dateExpressionString))
                return null;

            var dateExpressions = dateExpressionString.Split('|');

            if (dateExpressions.Count() == 0)
                return null;

            Expression<Func<File<IReplay>, bool>> filterExpression = null;
            var replay = Expression.Parameter(typeof(File<IReplay>), "r");

            foreach (var dateExpression in dateExpressions)
            {
                // where(r => r.Content.TimeStamp ><= parsedDate)
                DateTime?[] parsedDates = ParseDate(dateExpression);

                if (parsedDates == null || parsedDates.Count() == 0 || parsedDates.Any(d => d == null))
                    return null;

                Expression replayDate = Expression.Call(
                    typeof(ReplayFilterer).GetMethod(nameof(ResetTimePart), 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static), 
                    Expression.PropertyOrField(Expression.PropertyOrField(replay, "Content"), "TimeStamp"));

                Expression body = null;

                if (parsedDates.Count() > 1)
                {
                    // between, inclusive
                    body = Expression.AndAlso(
                        Expression.IsTrue(
                            Expression.GreaterThanOrEqual(
                                replayDate, Expression.Constant(parsedDates[0])
                            )
                        ),
                        Expression.IsTrue(
                            Expression.LessThanOrEqual(
                                replayDate, Expression.Constant(parsedDates[1])
                                )
                        )
                    );
                }
                else
                {
                    int? comparison = ParseComparison(dateExpression);
                    if (comparison == null)
                        return null;

                    // <= : -2, < : -1, = : 0, > : 1, >= 2
                    var replayFilter = Expression.Constant(parsedDates[0]);
                    //TODO extract to function

                    switch (comparison)
                    {
                        case -2:
                            body = Expression.LessThanOrEqual(replayDate, replayFilter);
                            break;
                        case -1:
                            body = Expression.LessThan(replayDate, replayFilter);
                            break;
                        case 0:
                            body = Expression.Equal(replayDate, replayFilter);
                            break;
                        case 1:
                            body = Expression.GreaterThan(replayDate, replayFilter);
                            break;
                        case 2:
                            body = Expression.GreaterThanOrEqual(replayDate, replayFilter);
                            break;
                        default:
                            throw new Exception();

                    }
                }

                filterExpression = CreateOrAddOrExpression(filterExpression, body, replay);
            }

            return filterExpression?.Compile();
        }

        private static DateTime ResetTimePart(DateTime dateTime)
        {
            long ticks = dateTime.TimeOfDay.Ticks;
            return dateTime.AddTicks(-ticks);
        }

        private DateTime?[] ParseDate(string dateExpression)
        {
            if (string.IsNullOrWhiteSpace(dateExpression))
                return null;

            if (_timeRangeRegex.IsMatch(dateExpression))
            {
                var matches = _timeRangeRegex.Matches(dateExpression);
                if (matches.Count == 0 || matches.Count > 1)
                    return null;

                DateTime?[] dates = new DateTime?[2];

                for (int i = 0; i < 2; i++)
                {
                    var dateValue = matches[0].Groups[i + 1];
                    dates[i] = ParseDatevalue(dateValue.Value);
                }

                return dates;
            }
            else if (_writtenDateWithoutQuantifierRegex.IsMatch(dateExpression))
            {
                // this year | last year | ... are also time ranges
                var now = DateTime.Now;
                switch (dateExpression)
                {
                    case "this year":
                        return new DateTime?[2] { ResetTimePart(ToStartOfYear(now)), ResetTimePart(now) };
                    case "last year":
                        return new DateTime?[2] { ResetTimePart(ToStartOfYear(now.AddYears(-1))), ResetTimePart(ToStartOfYear(now).AddDays(-1)) };
                    case "this month":
                        return new DateTime?[2] { new DateTime(now.Year, now.Month, 1), ResetTimePart(now) };
                    case "last month":
                        var startLastMonth = ResetTimePart(new DateTime(now.Year, now.Month, 1).AddMonths(-1));
                        return new DateTime?[2] { startLastMonth, startLastMonth.AddDays(DateTime.DaysInMonth(startLastMonth.Year, startLastMonth.Month) - 1) };
                    case "this week":
                        return new DateTime?[2] { ResetTimePart(ToStartOfWeek(now)), ResetTimePart(now) };
                    case "last week":
                        return new DateTime?[2] { ResetTimePart(ToStartOfWeek(now).AddDays(-7)), ResetTimePart(ToStartOfWeek(now).AddDays(-1)) };
                    case "today":
                        return new DateTime?[2] { ResetTimePart(now), ResetTimePart(now) };
                    case "yesterday":
                        return new DateTime?[2] { ResetTimePart(now.AddDays(-1)), ResetTimePart(DateTime.Now)};
                    default:
                        {
                            var month = GetMonth(dateExpression);
                            if (!month.HasValue)
                                return null;

                            var monthDate = new DateTime(DateTime.Now.Year, month.Value, 1);

                            return new DateTime?[2] { monthDate, monthDate.AddMonths(1).AddDays(-1)};
                        }
                }
            }
            else if (_writtenPreviousDateWithQuantifierRegex.IsMatch(dateExpression))
            {
                //  the "previous 2 weeks" refers to the past 2 weeks counting back from the start of this week
                var match = _writtenPreviousDateWithQuantifierRegex.Match(dateExpression);
                if (match.Groups.Count != 3)
                    return null;

                int timeNumber;
                if (!int.TryParse(match.Groups[1].Value, out timeNumber))
                    return null;

                var timeUnit = match.Groups[2].Value;

                var dates = new DateTime?[2];
                var now = DateTime.Now;

                switch (timeUnit)
                {
                    case "years":
                        dates[0] = ResetTimePart(ToStartOfYear(now.AddYears(-timeNumber)));
                        dates[1] = ResetTimePart(ToStartOfYear(now).AddDays(-1));
                        break;
                    case "months":
                        dates[0] = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddMonths(-timeNumber);
                        dates[1] = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddDays(-1);
                        break;
                    case "weeks":
                        dates[0] = ResetTimePart(ToStartOfWeek(now).AddDays(-timeNumber * 7));
                        dates[1] = ResetTimePart(ToStartOfWeek(now).AddDays(-1));
                        break;
                    case "days":
                        dates[0] = ResetTimePart(DateTime.Now.AddDays(-timeNumber));
                        dates[1] = ResetTimePart(DateTime.Now.AddDays(-1));
                        break;
                    default:
                        return null;
                }

                for (int i = 0; i < 2; i++)
                {
                    if (!EnsureValidDate(dates[i]))
                        return null;
                }

                return dates;
            }
            else
            {
                return new DateTime?[1] { ParseDatevalue(dateExpression) };
            }
        }

        private int? GetMonth(string dateExpression)
        {
            dateExpression = dateExpression.ToLower();
            switch (dateExpression)
            {
                case "january":
                    return 1;
                case "february":
                    return 2;
                case "march":
                    return 3;
                case "april":
                    return 4;
                case "may":
                    return 5;
                case "june":
                    return 6;
                case "july":
                    return 7;
                case "august":
                    return 8;
                case "september":
                    return 9;
                case "october":
                    return 10;
                case "november":
                    return 11;
                case "december":
                    return 12;
                default:
                    return null;
            }
        }

        private DateTime ToStartOfYear(DateTime dateTime)
        {
            return dateTime.AddDays(-(dateTime.DayOfYear - 1));
        }

        private DateTime? ParseDatevalue(string dateValue)
        {
            if (string.IsNullOrWhiteSpace(dateValue))
                return null;

            dateValue = RemoveComparisonOperator(dateValue);

            Match match = null;

            if (_digitalYearAndMonthAndDayRegex.IsMatch(dateValue))
            {
                match = _digitalYearAndMonthAndDayRegex.Match(dateValue);

                // years, months, days
                int[] dateParts = new int[3];
                for (int i = match.Groups.Count - 1, j = 2; i > 0; i--, j--)
                {
                    int.TryParse(match.Groups[i].Value, out dateParts[j]);
                }

                if (!EnsureValidDate(dateParts))
                    return null;

                return new DateTime(dateParts[0], dateParts[1], dateParts[2]);
            }
            else if (_writtenCombinableDateRegex.IsMatch(dateValue))
            {
                if (_writtenAgoDateWithQuantifierRegex.IsMatch(dateValue))
                {
                    match = _writtenAgoDateWithQuantifierRegex.Match(dateValue);

                    var now = ResetTimePart(DateTime.Now);
                    var timeSpan = ParseTimeSpan(match.Groups["WithAgo"].Value, dateValue);
                    if (!timeSpan.HasValue)
                        return null;

                    return now.Add(timeSpan.Value.Negate());
                }
                else
                {
                    // 1 week = 7 days, 1 month = 31 days, 1 year = 365 days
                    match = _writtenCombinableDateRegex.Match(dateValue);
                    var originalNow = ResetTimePart(DateTime.Now);
                    var now = originalNow;

                    var groups = ExtractAgoDates(match);
                    if (groups == null)
                        return null;

                    for (int i = 0; i < groups.Count; i++)
                    {
                        if (string.IsNullOrWhiteSpace(groups[i].Value))
                            continue;

                        for (int j = 0; j < groups[i].Captures.Count; j++)
                        {
                            var timeSpan = ParseTimeSpan(groups[i].Captures[j].Value, dateValue);
                            if (!timeSpan.HasValue)
                                continue;

                            now = now.Add(timeSpan.Value.Negate());
                        }
                    }
                    return (originalNow == now) ? (DateTime?)null : now;
                }
            }
            else
            {
                return null;
            }
        }

        private List<Group> ExtractAgoDates(Match match)
        {
            List<Group> groups = new List<Group>();
            var FirstWithoutAgo = match.Groups["FirstWithoutAgo"];
            var VariableWithoutAgo = match.Groups["VariableWithoutAgo"];
            var WithAgo = match.Groups["WithAgo"];

            if (FirstWithoutAgo == null || VariableWithoutAgo == null || WithAgo == null)
                return null;

            groups.Add(FirstWithoutAgo);
            groups.Add(VariableWithoutAgo);
            groups.Add(WithAgo);

            return groups;
        }

        private static readonly Regex _number = new Regex(@"\d+");
        private TimeSpan? ParseTimeSpan(string timeSpan, string dateValue)
        {
            if (string.IsNullOrWhiteSpace(timeSpan))
                return null;

            if (!_number.IsMatch(timeSpan))
                return null;

            var timeNumberMatch = _number.Match(timeSpan);
            int timeNumber;
            if (!int.TryParse(timeNumberMatch.Value, out timeNumber))
                return null;

            if (timeNumber < 0)
                return null;

            var timeUnit = timeSpan.Substring(timeNumberMatch.Index + timeNumberMatch.Length).Trim(' ');
            switch (timeUnit)
            {
                case "years":
                    return new TimeSpan(timeNumber * 365, 0, 0, 0, 0);
                case "months":
                    return new TimeSpan(timeNumber * 31, 0, 0, 0, 0);
                case "weeks":
                    return new TimeSpan(timeNumber * 7, 0, 0, 0, 0);
                case "days":
                    return new TimeSpan(timeNumber, 0, 0, 0, 0);
                default:
                    return null;
            }
        }

        private DateTime ToStartOfWeek(DateTime dateTime)
        {
            var dayOfWeek = dateTime.DayOfWeek;
            return dateTime.AddDays(-((((int)dayOfWeek) + 6) % 7));
        }

        private bool EnsureValidDate(DateTime? date)
        {
            if (!date.HasValue)
                return false;

            var dateValue = date.Value;

            return EnsureValidDate(new int[3]
            {
                dateValue.Year,
                dateValue.Month,
                dateValue.Day
            });
        }

        private bool EnsureValidDate(int[] dateParts)
        {
            if (dateParts[0] < 1998)
                return false;

            if (dateParts[1] < 0 || dateParts[2] < 0)
                return false;

            if (dateParts[1] == 0)
                dateParts[1] = 1;

            if (dateParts[2] == 0)
            {
                dateParts[2] = 1;
            }
            else 
            {
                dateParts[2] = Math.Max(dateParts[2], DateTime.DaysInMonth(dateParts[0], dateParts[1]));
            }

            return true;
        }

        private Func<File<IReplay>, bool> ParsePlayerFilter(string replayExpressionString)
        {
            var replayExpressions = replayExpressionString.Split(new char[] { '|' });

            if (replayExpressions.Count() == 0)
                return null;

            Expression<Func<File<IReplay>, bool>> filterExpression = null;
            //TODO i don't think it's necessary to name your parameters...
            var replay = Expression.Parameter(typeof(File<IReplay>), "r");
            Expression body = null;
            var replayContent = Expression.PropertyOrField(replay, "Content");
            Expression replayPlayers = Expression.PropertyOrField(replayContent, "Players");
            Expression replayWinners = Expression.PropertyOrField(replayContent, "Winners");
            MethodInfo any = GetExtensionMethod(typeof(Enumerable).Assembly, "Any", typeof(IEnumerable<>)).MakeGenericMethod(new Type[] { typeof(IPlayer) });
            MethodInfo asQueryable = GetExtensionMethod(typeof(Enumerable).Assembly, "AsQueryable", typeof(IEnumerable<>)).MakeGenericMethod(new Type[] { typeof(IPlayer) });
            MethodInfo isMatch = typeof(Regex).GetMethod("IsMatch", new Type[] { typeof(string) });

            foreach (var replayExpression in replayExpressions)
            {
                // playername, isWinner, race
                var playersProperties = ParsePlayerProperties(replayExpression);
                if (playersProperties == null)
                    return null;

                // List<IReplay> replaysList = new List<IReplay>();
                // List<string> requestePlayers = new List<string>();
                // replaysList.Where(r => requestedPlayers.All(pn => r.Players.Any(p => p.Name == pn.Name)));
                // replaysList.Where(r => r.Players.Intersect(requestePlayers).Count() == requestePlayers.Count());
                // FOR NOW ==> replaysList.Where(r => r.Players.Any(p => p.Name == "") && r.Players.Any(p1 => p1.Name == "")); <== FOR NOW
                // replaysList.Where(r => r.Players.Any(p => p.Name == "") && r.Players.Any(p => p.Name == ""));
                Expression<Func<File<IReplay>, bool>> singlePlayersExpression = null;
                foreach (var playerProperties in playersProperties)
                {
                    var player = Expression.Parameter(typeof(IPlayer), "p");
                    var playerName = Expression.PropertyOrField(player, "Name");
                    Expression playerRace = Expression.PropertyOrField(player, "RaceType");
                    Expression requestedPlayerName = Expression.Constant(new Regex(playerProperties.Item1, RegexOptions.IgnoreCase), typeof(Regex));

                    // iswinner
                    if (playerProperties.Item2.HasValue)
                    {
                        // replays.where(r => r.winners.Any(p => p.Name == "name"))
                        body = Expression.IsTrue(
                            Expression.Call(
                                any,
                                Expression.Call(
                                    asQueryable,
                                    replayWinners
                                ),
                                Expression.Lambda<Func<IPlayer, bool>>(
                                    Expression.IsTrue(
                                        Expression.Call(
                                            requestedPlayerName,
                                            isMatch,
                                            playerName
                                        )
                                    ),
                                    player
                                )
                            )
                        );
                    }
                    else
                    {
                        // replays.where(r => r.players.Any(p => p.Name == "name"))
                        body = Expression.IsTrue(
                            Expression.Call(
                                any,
                                Expression.Call(
                                    asQueryable,
                                    replayPlayers
                                ),
                                Expression.Lambda<Func<IPlayer, bool>>(
                                    Expression.IsTrue(
                                        Expression.Call(
                                            requestedPlayerName,
                                            isMatch,
                                            playerName
                                        )
                                    ),
                                    player
                                )
                            )
                        );
                    }

                    // race
                    if (playerProperties.Item3.HasValue)
                    {
                        var requestedRaceValue = ToRaceType(playerProperties.Item3.Value);
                        if (!requestedRaceValue.HasValue)
                            return null;

                        Expression requestedRace = Expression.Constant(requestedRaceValue.Value, typeof(ReplayParser.Entities.RaceType));
                        // replays.where(r => r.players.Any(p => p.Name && p.RaceType == "race"))
                        Expression raceExpression = Expression.Equal(
                            playerRace,
                            requestedRace
                        );

                        body = new AddAdditionalAndAlsoToMethodCallModifier(raceExpression, "Any").Modify(body);
                    }

                    //TODO fix, this order doesn't work, it's adding duplicates and adding AND conditions at the wrong time
                    singlePlayersExpression = CreateOrAddAndExpression(singlePlayersExpression, body, replay);
                }

                filterExpression = CreateOrAddOrExpression(filterExpression, singlePlayersExpression.Body, replay);
            }
            return filterExpression?.Compile();
        }

        private MethodInfo GetExtensionMethod(Assembly assembly, string methodName, Type type)
        {
            return assembly.GetTypes()
                .Where(t => t.IsSealed && !t.IsGenericType && !type.IsNested)
                    .SelectMany(t => t.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                    .Where(m => m.Name == methodName))
                .FirstOrDefault();
        }

        private ReplayParser.Entities.RaceType? ToRaceType(RaceType raceType)
        {
            switch (raceType)
            {
                case RaceType.Zerg:
                    return ReplayParser.Entities.RaceType.Zerg;
                case RaceType.Protoss:
                    return ReplayParser.Entities.RaceType.Protoss;
                case RaceType.Terran:
                    return ReplayParser.Entities.RaceType.Terran;
                default:
                    return null;
            }
        }

        private static readonly string _andOperator = "\\s+&\\s+";
        private static readonly string _isWinnerPattern = "iswinner(?:\\s?=\\s?(true|false))?";
        private static readonly string _racePattern = "race=(zerg|protoss|terran|z|t|p)";
        private static readonly string _playerPropertyPattern = $"([^\\s]+)(?:(?:{_andOperator}({_isWinnerPattern}))?(?:{_andOperator}({_racePattern}))?|(?:{_andOperator}({_racePattern}))?(?:{_andOperator}({_isWinnerPattern}))?)";
        private static readonly Regex _isWinnerRegex = new Regex(_isWinnerPattern, RegexOptions.IgnoreCase);
        private static readonly Regex _raceRegex = new Regex(_racePattern, RegexOptions.IgnoreCase);
        private static readonly Regex _playerPropertyRegex = new Regex(_playerPropertyPattern, RegexOptions.IgnoreCase);

        private Tuple<string, bool?, RaceType?>[] ParsePlayerProperties(string playerExpressionString)
        {
            if (string.IsNullOrWhiteSpace(playerExpressionString))
                return null;

            var playerExpressions = playerExpressionString.Split(',').Select(p => p.Trim(' ')).ToArray();
            Tuple<string, bool?, RaceType?>[] playerProperties = new Tuple<string, bool?, RaceType?>[playerExpressions.Length];

            for (int i = 0; i < playerExpressions.Count(); i++)
            {
                if (!_playerPropertyRegex.IsMatch(playerExpressions[i]))
                    return null;

                var match = _playerPropertyRegex.Match(playerExpressions[i]);

                string playerName = match.Groups[1].Value;
                bool? isWinner = null;
                RaceType? race = null;

                for (int j = 2; j < match.Groups.Count; j++)
                {
                    if (string.IsNullOrWhiteSpace(match.Groups[j].Value))
                        continue;

                    if (isWinner == null && _isWinnerRegex.IsMatch(match.Groups[j].Value))
                    {
                        var winnerMatch = _isWinnerRegex.Match(match.Groups[j].Value);
                        isWinner = string.IsNullOrWhiteSpace(winnerMatch.Groups[j].Value) ? true : bool.Parse(winnerMatch.Groups[j].Value);
                    }

                    if (race == null && _raceRegex.IsMatch(match.Groups[j].Value))
                    {
                        var raceMatch = _raceRegex.Match(match.Groups[j].Value);
                        var raceString = raceMatch.Groups[1].Value;
                        RaceType raceValue;
                        if (raceString.Length == 1)
                        {
                            raceString = ToLongRaceForm(raceString);
                        }

                        if (!Enum.TryParse(raceString, true, out raceValue))
                        {
                            return null;
                        }
                        race = raceValue;
                    }
                }
                playerProperties[i] = new Tuple<string, bool?, RaceType?>(playerName, isWinner, race);
            }

            return playerProperties;
        }

        private string ToLongRaceForm(string singleLetterRace)
        {
            if (string.IsNullOrWhiteSpace(singleLetterRace))
                return null;

            singleLetterRace = singleLetterRace.ToLower();
            switch(singleLetterRace)
            {
                case "z":
                    return "zerg";
                case "p":
                    return "protoss";
                case "t":
                    return "terran";
                default:
                    return null;
            }
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
                var replayTeams = Expression.New(typeof(Teams).GetConstructor(new Type[] { typeof(IReplay) }), replayProper);
                var replayMatchup = Expression.New(typeof(MatchUp).GetConstructor(new Type[] { typeof(IReplay), typeof(Teams) }), replayProper, replayTeams);
                var replayMatchupAsString = Expression.Call(replayMatchup, typeof(MatchUp).GetMethod("GetSection"), Expression.Constant(string.Empty, typeof(string)));
                // where(r => CompareMatchups(new matchup(new team(replay), replay).GetSection(), matchupstring))
                Expression body = Expression.IsTrue(
                    Expression.TryCatch(
                        Expression.Call(
                            typeof(ReplayFilterer).GetMethod(nameof(ReplayFilterer.CompareMatchups), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static),
                            replayMatchupAsString,
                            Expression.Constant(matchupExpression, typeof(string))
                        ),
                        Expression.Catch(typeof(Exception), Expression.Constant(false))
                    )
                );

                filterExpression = CreateOrAddOrExpression(filterExpression, body, replay);
            }

            return filterExpression.Compile();
        }

        //TODO: refactor
        private static bool CompareMatchups(string matchupA, string matchupExpression)
        {
            var matchupAStrings = GetMatchups(matchupA);
            var matchupBStrings = GetMatchups(matchupExpression);

            if (matchupAStrings == null || matchupBStrings == null)
                return false;

            if (matchupAStrings.Count() != matchupBStrings.Count())
                return false;

            int[][] teamRaceCountsA = new int[matchupAStrings.Count()][];
            int[][] teamRaceCountsB = new int[matchupBStrings.Count()][];

            for (int i = 0; i < matchupAStrings.Count(); i++)
            {
                teamRaceCountsA[i] = GetRaceCounts(matchupAStrings[i]);
            }

            for (int i = 0; i < matchupBStrings.Count(); i++)
            {
                teamRaceCountsB[i] = GetRaceCounts(matchupBStrings[i], true);
            }

            bool[] matchedTeams = new bool[teamRaceCountsA.Count()];

            for (int i = 0; i < teamRaceCountsA.Count(); i++)
            {
                var matchingTeamNumbers = FindMatchingTeams(teamRaceCountsA[i], teamRaceCountsB);
                if (matchingTeamNumbers == null || matchingTeamNumbers.Count() == 0)
                    return false;


                var notYetMatched = matchingTeamNumbers.Where(candidateMatch => candidateMatch != -1 && matchedTeams[candidateMatch] == false);
                if (notYetMatched.Count() == 0)
                    return false;

                matchedTeams[notYetMatched.First()] = true;
            }

            return true;
        }

        private static int[] FindMatchingTeams(int[] raceCountsTeamA, int[][] matchupRaceCountsB)
        {
            if (raceCountsTeamA == null || matchupRaceCountsB == null)
                return null;

            if (matchupRaceCountsB.Count() == 0)
                return null;

            int[] matchedTeams = new int[matchupRaceCountsB.Count()];

            for (int i = 0; i < matchupRaceCountsB.Count(); i++)
            {
                if (TeamsEqual(raceCountsTeamA, matchupRaceCountsB[i]))
                {
                    matchedTeams[i] = i;
                }
                else
                {
                    matchedTeams[i] = -1; 
                }
            }

            return matchedTeams;
        }

        private static bool TeamsEqual(int[] raceCountsTeamWithoutWildCards, int[] raceCountsTeamWithWildCards)
        {
            if (raceCountsTeamWithoutWildCards == null || raceCountsTeamWithWildCards == null)
                return false;

            if (raceCountsTeamWithWildCards.Length < raceCountsTeamWithoutWildCards.Length)
                return TeamsEqual(raceCountsTeamWithWildCards, raceCountsTeamWithoutWildCards);

            int numberOfWildCards = raceCountsTeamWithWildCards[4];

            for (int i = 0; i < raceCountsTeamWithoutWildCards.Length; i++)
            {
                if (raceCountsTeamWithoutWildCards[i] != raceCountsTeamWithWildCards[i] && --numberOfWildCards < 0)
                    return false;
            }
            return true;
        }

        private static readonly string _versusPattern = "vs?";
        private static readonly Regex _versus = new Regex(_versusPattern, RegexOptions.IgnoreCase);

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
                matchupA = matchupA.Remove(0, match.Index + match.Value.Length);
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
        private static readonly string _lessThanGreaterThanOperatorsPattern = "^(<(?!=)|<=|=|>(?!=)|>=)?";
        private static readonly string _digitalMinutesSecondsPattern = "^(\\d{2}):(\\d{2})$";
        private static readonly string _digitalHoursMinutesSecondsPattern = "^(\\d{2}):(\\d{2}):(\\d{2})$";
        private static readonly string _writtenHoursMinutesSecondsPattern = "^(?:(\\d+)(?:h(?:rs|hours)?))?(?:(\\d+)(?:m(?:in(?:utes)?)?))?(?:(\\d+)(?:s(?:ec(?:onds)?)?))?";
        private static readonly string _timeRangePattern = "^between\\s*(.*?)\\s+and\\s+(.*)$";
        private static readonly Regex _lessThanGreaterThanOperatorsRegex = new Regex(_lessThanGreaterThanOperatorsPattern);
        private static readonly Regex _digitalMinutesSecondsRegex = new Regex(_digitalMinutesSecondsPattern);
        private static readonly Regex _digitalHoursMinutesSecondsRegex = new Regex(_digitalHoursMinutesSecondsPattern);
        private static readonly Regex _writtenHoursMinutesSecondsRegex = new Regex(_writtenHoursMinutesSecondsPattern, RegexOptions.IgnoreCase);
        private static readonly Regex _timeRangeRegex = new Regex(_timeRangePattern, RegexOptions.IgnoreCase);

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
                    return null;

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
                        return null;

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
            return filterExpression?.Compile();
        }

        //TODO extract to constants class or something
        private const double FastestFPS = (double)1000 / 42;

        private int?[]  ParseDuration(string durationExpressionString)
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

                var mapRegex = new Regex(map, RegexOptions.IgnoreCase);

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

            var newParameters = expression.Parameters.ToList().Union(parameters);

            return expression.Update(
                    Expression.Or(
                        expression.Body,
                        body
                ),
                newParameters
            );
            // return Expression.Lambda<Func<T1, T2>>(Expression.Or(expression, Expression.Lambda<Func<T1, T2>>(body, parameters)));
        }

        private Expression<Func<T1, T2>> CreateOrAddAndExpression<T1, T2>(Expression<Func<T1, T2>> expression, Expression body, params ParameterExpression[] parameters)
        {
            if (expression == null)
                return Expression.Lambda<Func<T1, T2>>(body, parameters);

            var newParameters = expression.Parameters.ToList().Union(parameters);

            return expression.Update(
                Expression.AndAlso(
                    expression.Body,
                    body
                ),
                newParameters
            );
        }
    }
}
