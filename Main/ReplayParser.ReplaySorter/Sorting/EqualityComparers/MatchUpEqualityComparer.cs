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
            Dictionary<IDictionary<RaceType, int>, int> yRaceCombinationToTeam = y.ToDictionary(t => t.Value, t => t.Key);
            bool[] teamMatched = new bool[y.Count];

            foreach (var team in x)
            {
                if (!y.Values.Contains(team.Value, RaceCombinationEq))
                {
                    return false;
                }
                else
                {
                    var yTeam = yRaceCombinationToTeam[team.Value];
                    if (teamMatched[yTeam])
                        return false;
                    else
                        teamMatched[yTeam] = true;
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
