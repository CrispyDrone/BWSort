using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ReplayParser.ReplaySorter.Sorting
{
    class MatchUpEqualityComparer : IEqualityComparer<IDictionary<int, IDictionary<RaceType, int>>>
    {
        private RaceCombinationEqualityComparer _raceEq = new RaceCombinationEqualityComparer();

        public bool Equals(IDictionary<int, IDictionary<RaceType, int>> x, IDictionary<int, IDictionary<RaceType, int>> y)
        {
            if (x == null || y == null)
            {
                return false;
            }
            if (x.Count != y.Count)
            {
                return false;
            }

            // Overcomplicated??
            Dictionary<IDictionary<RaceType, int>, int> yRaceCombinationToCounts = y
                .GroupBy(t => t.Value, _raceEq)
                .Select(raceCombinationGroup => new KeyValuePair<IDictionary<RaceType, int>, int>(raceCombinationGroup.Key, raceCombinationGroup.Select(g => g.Key).Aggregate(0, (count, team) => ++count)))
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value, _raceEq);

            foreach (var raceCombination in x.Values)
            {
                if (!yRaceCombinationToCounts.ContainsKey(raceCombination))
                {
                    return false;
                }
                else
                {
                    if (yRaceCombinationToCounts[raceCombination] <= 0)
                        return false;

                    yRaceCombinationToCounts[raceCombination] = yRaceCombinationToCounts[raceCombination]--;
                }
            }
            return true;
        }

        public int GetHashCode(IDictionary<int, IDictionary<RaceType, int>> obj)
        {
            // ???
            var TeamsHash = obj.Keys.Count.GetHashCode();
            var RaceCombinationsHash = 0;
            foreach (var RaceCombination in obj.Values)
            {
                RaceCombinationsHash += _raceEq.GetHashCode(RaceCombination);
            }
            return TeamsHash + RaceCombinationsHash; 
        }
    }
}
