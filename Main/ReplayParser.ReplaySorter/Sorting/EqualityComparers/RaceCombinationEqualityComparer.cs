using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ReplayParser.ReplaySorter.Sorting
{
    class RaceCombinationEqualityComparer : IEqualityComparer<IDictionary<RaceType, int>>
    {
        public bool Equals(IDictionary<RaceType, int> x, IDictionary<RaceType, int> y)
        {
            if (x == null || y == null)
            {
                return false;
            }
            if (x.Count != y.Count)
            {
                return false;
            }
            foreach (var race in x)
            {
                if (race.Value != y[race.Key])
                {
                    return false;
                }
            }
            return true;
        }

        public int GetHashCode(IDictionary<RaceType, int> obj)
        {
            // ???
            int hashcode = 0;
            foreach (var race in obj)
            {
                if (race.Key == RaceType.Zerg)
                {
                    hashcode += race.Value.GetHashCode() * 3;
                }
                else if (race.Key == RaceType.Terran)
                {
                    hashcode += race.Value.GetHashCode() * 7;
                }
                else if (race.Key == RaceType.Protoss)
                {
                    hashcode += race.Value.GetHashCode() * 13;
                }
            }
            return hashcode;
        }
    }
}
