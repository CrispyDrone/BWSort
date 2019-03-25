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
            Predicate<File<IReplay>>[] queries = ParseFilters(filterExpressions);
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

        private Predicate<File<IReplay>>[] ParseFilters(Dictionary<int, string> filterExpressions)
        {
            Predicate<File<IReplay>>[] predicates = new Predicate<File<IReplay>>[filterExpressions.Count];
            int counter = 0;

            foreach (var filterExpression in filterExpressions)
            {
                switch (filterExpression.Key)
                {
                    case MAPCODE:
                        predicates[counter] = ParseMapFilter(filterExpression.Value);
                        break;
                    case DURATIONCODE:
                        predicates[counter] = ParseDurationFilter(filterExpression.Value);
                        break;
                    case MATCHUPCODE:
                        predicates[counter] = ParseMatchupFilter(filterExpression.Value);
                        break;
                    case PLAYERCODE:
                        predicates[counter] = ParsePlayerFilter(filterExpression.Value);
                        break;
                    case DATECODE:
                        predicates[counter] = ParseDateFilter(filterExpression.Value);
                        break;
                    default:
                        throw new Exception();
                }
                counter++;
            }
        }
        private List<File<IReplay>> ApplyTo(List<File<IReplay>> list, Predicate<File<IReplay>>[] queries)
        {
            IQueryable<File<IReplay>> filteredList = new List<File<IReplay>>(list).AsQueryable();

            for (int i = 0; i < queries.Length; i++)
            {
                filteredList = filteredList.Where(r => queries[i](r));
            }
            return filteredList.ToList();
        }

        private Predicate<File<IReplay>> ParseDateFilter(string value)
        {
            throw new NotImplementedException();
        }

        private Predicate<File<IReplay>> ParsePlayerFilter(string value)
        {
            throw new NotImplementedException();
        }

        private Predicate<File<IReplay>> ParseMatchupFilter(string value)
        {
            throw new NotImplementedException();
        }

        private Predicate<File<IReplay>> ParseDurationFilter(string value)
        {
            throw new NotImplementedException();
        }

        // Why am i using a predicate? Can't I just use a Func instead?
        private Predicate<File<IReplay>> ParseMapFilter(string mapExpression)
        {
            Predicate<File<IReplay>> predicate = null;
            Expression<Func<File<IReplay>, bool>> filterExpression = null;

            var maps = mapExpression.Split(new char[] { '|' });
            foreach (var map in maps)
            {
                char firstChar = map[0];
                char lastChar = map[map.Length - 1];
                string middlePartMapName = map.Substring(1, map.Length - 2);
                string escapedMapName = firstChar + Regex.Escape(middlePartMapName) + lastChar;
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
                if (predicate == null)
                {
                    filterExpression = Expression.Lambda<Func<File<IReplay>, bool>>(body, replay);
                }
                else
                {
                    filterExpression = Expression.Lambda<Func<File<IReplay>, bool>>(Expression.Or(filterExpression, Expression.Lambda<Func<File<IReplay>, bool>>(body, replay)));
                }
            }

            return new Predicate<File<IReplay>>(filterExpression.Compile());
        }
    }
}
