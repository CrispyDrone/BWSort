using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ReplayParser.ReplaySorter.Sorting
{
    class MatchUpEqualityComparer : IEqualityComparer<IDictionary<int, IDictionary<RaceType, int>>>
    {
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
            RaceCombinationEqualityComparer RaceCombinationEq = new RaceCombinationEqualityComparer();
            foreach (var team in x)
            {
                if (!y.Values.Contains(team.Value, RaceCombinationEq))
                {
                    return false;
                }
            }
            return true;
        }

        public int GetHashCode(IDictionary<int, IDictionary<RaceType, int>> obj)
        {
            // ???
            var TeamsHash = obj.Keys.Count.GetHashCode();
            var RaceCombinationsHash = 0;
            RaceCombinationEqualityComparer RaceEq = new RaceCombinationEqualityComparer();
            foreach (var RaceCombination in obj.Values)
            {
                RaceCombinationsHash += RaceEq.GetHashCode(RaceCombination);
            }
            return TeamsHash + RaceCombinationsHash; 
        }
    }
}
