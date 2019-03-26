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
        private static readonly string _digitalHoursMinutesPattern = "(\\d{2}):(\\d{2})$";
        private static readonly string _digitalHoursMinutesSecondsPattern = "(\\d{2}):(\\d{2}):(\\d{2})$";
        private static readonly string _writtenHoursMinutesSecondsPattern =  "(\\d+(h(?:rs)?|hours)?(\\d+m(?:in(?:utes)?)?)?(\\d+s(?:ec(?:onds)?)?)?%";
        private static readonly string _timeRangePattern = "^between\\s*(.*?)-(.*)$";
        private static readonly Regex _digitalHourMinutesRegex = new Regex(_digitalHoursMinutesPattern);
        private static readonly Regex _digitalHourMinutesSeconds = new Regex(_digitalHoursMinutesSecondsPattern);
        private static readonly Regex _writtenHourMinutesSeconds = new Regex(_writtenHoursMinutesSecondsPattern);

        private Func<File<IReplay>, bool> ParseDurationFilter(string durationExpression)
        {
            Expression<Func<File<IReplay>, bool>> filterExpression = null;
            var dates = durationExpression.Split(new char[] { '|' });

            foreach (var date in dates)
            {
                TimeSpan[] durations = ParseDuration(date);
                var replay = Expression.Parameter(typeof(File<IReplay>), "r");
                Expression body = null;
                
                if (durations.Count() > 1)
                {
                    // between, inclusive...
                    body = Expression.AndAlso(
                        Expression.IsTrue(
                                Expression.GreaterThanOrEqual(
                                    Expression.PropertyOrField(replay, "Content.Duration"), Expression.Constant(durations[0])
                            )
                        ),
                        Expression.IsTrue(
                            Expression.LessThanOrEqual(
                                Expression.PropertyOrField(replay, "Content.Duration"), Expression.Constant(durations[1])
                                )
                        )
                    );
                }
                else
                {
                    int comparison = ParseComparison(date);
                    // <= : -2, < : -1, = : 0, > : 1, >= 2
                    var replayDuration = Expression.PropertyOrField(replay, "Content.Duration");
                    var replayFilter = Expression.Constant(durations[0]);
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

        private int ParseComparison(string date)
        {
            throw new NotImplementedException();
        }

        private TimeSpan[] ParseDuration(string date)
        {
            throw new NotImplementedException();
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
