using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReplayParser.ReplaySorter.Filtering
{
    public abstract class SimpleFilter<T> : Filter<T>
    {
        public override void Add(Filter<T> filter)
        {
            throw new InvalidOperationException("Unable to add child filter to SimpleFilter.");
        }

        public override void Remove(Filter<T> filter)
        {
            throw new InvalidOperationException("Unable to remove child filter from SimpleFilter.");
        }
    }
}
