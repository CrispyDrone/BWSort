using ReplayParser.Interfaces;
using ReplayParser.ReplaySorter.IO;
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

        private readonly Regex MAPFILTERLABEL = new Regex("m:");
        private readonly Regex DURATIONFILTERLABEL = new Regex("du:");
        private readonly Regex MATCHUPFILTERLABEL = new Regex("mu:");
        private readonly Regex PLAYERFILTERLABEL = new Regex("p:");
        private readonly Regex DATEFILTERLABEL = new Regex("d:");

        private readonly Regex FILTERLABEL = new Regex("^([md]u?|[pd]):");

        public List<File<IReplay>> Apply(List<File<IReplay>> list, string filterExpression)
        {
            Dictionary<int, string> filterExpressions = ExtractFilters(filterExpression);
            Func<File<IReplay>, bool>[] queries = ParseFilters(filterExpressions);
            return ApplyTo(list, queries);
        }

        private Dictionary<int, string> ExtractFilters(string filterExpression)
        {
            throw new NotImplementedException();
            // var matches = FILTERLABEL.Matches(filterExpression);
            // if (matches.Count == 0)
            //     return list;

            // foreach (Match match in matches)
            // {
            // }
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
                        throw new Exception();
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
                filteredList = filteredList.Where(r => queries[i](r));
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

        private Func<File<IReplay>, bool> ParseMatchupFilter(string value)
        {
            throw new NotImplementedException();
        }

        // number - timeunit [ - number - timeunit ]1-2
        // classical 05:10, 00:20:17
        private static readonly string _lessThanGreaterThanOperatorsPattern = "^(<|<=|>|>=)?";
        private static readonly string _digitalHoursMinutesPattern = "^(\\d{2}):(\\d{2})$";
        private static readonly string _digitalHoursMinutesSecondsPattern = "^(\\d{2}):(\\d{2}):(\\d{2})$";
        private static readonly string _writtenHoursMinutesSecondsPattern =  "^(\\d+(h(?:rs)?|hours)?(\\d+m(?:in(?:utes)?)?)?(\\d+s(?:ec(?:onds)?)?)?%";
        private static readonly string _timeRangePattern = "^between\\s*(.*?)-(.*)$";
        private static readonly Regex _lessThanGreaterThanOperatorsRegex = new Regex(_lessThanGreaterThanOperatorsPattern);
        private static readonly Regex _digitalHoursMinutesRegex = new Regex(_digitalHoursMinutesPattern);
        private static readonly Regex _digitalHoursMinutesSecondsRegex = new Regex(_digitalHoursMinutesSecondsPattern);
        private static readonly Regex _writtenHoursMinutesSecondsRegex = new Regex(_writtenHoursMinutesSecondsPattern);
        private static readonly Regex _timeRangeRegex = new Regex(_timeRangePattern);

        private Func<File<IReplay>, bool> ParseDurationFilter(string durationExpressionString)
        {
            Expression<Func<File<IReplay>, bool>> filterExpression = null;
            var durationExpressions = durationExpressionString.Split(new char[] { '|' });

            if (durationExpressions == null || durationExpressions.Count() == 0)
                return null;

            foreach (var durationExpression in durationExpressions)
            {
                int?[] durationsAsFrameCounts = ParseDuration(durationExpression);

                if (durationsAsFrameCounts == null || durationsAsFrameCounts.Count() == 0 || durationsAsFrameCounts.Any(f => f == null))
                    continue;

                var replay = Expression.Parameter(typeof(File<IReplay>), "r");
                var replayDuration = Expression.PropertyOrField(replay, "Content.FrameCount");
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

                //TODO extract to function
                // if (filterExpression == null)
                // {
                //     filterExpression = Expression.Lambda<Func<File<IReplay>, bool>>(body, replay);
                // }
                // else
                // {
                //     filterExpression = Expression.Lambda<Func<File<IReplay>, bool>>(Expression.Or(filterExpression, Expression.Lambda<Func<File<IReplay>, bool>>(body, replay)));
                // }
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
                int i = 0;

                foreach (Capture timeValue in matches[0].Captures)
                {
                    durationExpressionsAsFrameCount[i] = ParseTimevalue(timeValue.Value);
                    i++;
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
            //TODO extract common logic
            timeValue = RemoveComparisonOperator(timeValue);
            int frames = 0;
            if(_writtenHoursMinutesSecondsRegex.IsMatch(timeValue))
            {
                //TODO make helper method "EnsureSingleMatch" or something
                var match = _writtenHoursMinutesSecondsRegex.Match(timeValue);
                int hours, minutes, seconds = 0;
                if (!int.TryParse(match.Captures[0].Value, out hours))
                    throw new Exception();
                if (!int.TryParse(match.Captures[1].Value, out minutes))
                    throw new Exception();
                if (!int.TryParse(match.Captures[2].Value, out seconds))
                    throw new Exception();

                //TODO make helper function to calculate frames from time value
                 frames = (int)((hours * 3600 + minutes * 60 + seconds) * FastestFPS);
            }
            else if (_digitalHoursMinutesSecondsRegex.IsMatch(timeValue))
            {
                var match = _digitalHoursMinutesSecondsRegex.Match(timeValue);
                int hours, minutes, seconds = 0;
                if (!int.TryParse(match.Captures[0].Value, out hours))
                    throw new Exception();
                if (!int.TryParse(match.Captures[1].Value, out minutes))
                    throw new Exception();
                if (!int.TryParse(match.Captures[2].Value, out seconds))
                    throw new Exception();

                //TODO make helper function to calculate frames from time value
                 frames = (int)((hours * 3600 + minutes * 60 + seconds) * FastestFPS);
            }
            else if (_digitalHoursMinutesRegex.IsMatch(timeValue))
            {
                var match = _digitalHoursMinutesRegex.Match(timeValue);

                int hours, minutes = 0;
                if (!int.TryParse(match.Captures[0].Value, out hours))
                    throw new Exception();
                if (!int.TryParse(match.Captures[1].Value, out minutes))
                    throw new Exception();

                //TODO make helper function to calculate frames from time value
                 frames = (int)((hours * 3600 + minutes * 60) * FastestFPS);
            }
            else
            {
                // reevaluate whether you should be throwing exceptions... this is not an exceptional state. See if you can return nulls instead
                return null;
            }
            return frames;
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
            Expression<Func<File<IReplay>, bool>> filterExpression = null;
            var maps = mapExpression.Split(new char[] { '|' });

            foreach (var map in maps)
            {
                string escapedMapName = EscapeExceptValidWildcards(map);
                var mapRegex = new Regex(escapedMapName);

                // where(r => MapRegex.IsMatch(r.ReplayMap.MapName))
                var replay = Expression.Parameter(typeof(File<IReplay>), "r");
                var body = Expression.IsTrue(
                    Expression.Call(
                        Expression.Constant(mapRegex),
                        typeof(Regex).GetMethod("IsMatch"),
                        Expression.PropertyOrField(replay, "Content.ReplayMap.MapName")
                    )
                );

                if (filterExpression == null)
                {
                    filterExpression = Expression.Lambda<Func<File<IReplay>, bool>>(body, replay);
                }
                else
                {
                    filterExpression = Expression.Lambda<Func<File<IReplay>, bool>>(Expression.Or(filterExpression, Expression.Lambda<Func<File<IReplay>, bool>>(body, replay)));
                }
            }

            return filterExpression.Compile();
        }

        private string EscapeExceptValidWildcards(string searchString)
        {
            char firstChar = searchString[0];
            char lastChar = searchString[searchString.Length - 1];
            string middlePartSearchString = searchString.Substring(1, searchString.Length - 2);
            return firstChar + Regex.Escape(middlePartSearchString) + lastChar;
        }
    }
}
