using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReplayParser.ReplaySorter.Filtering
{
    public abstract class ComplexFilter<T> : Filter<T>
    {
        private IList<Filter<T>> _filters;

        public override bool IsComplex => true;

        public override void Add(Filter<T> filter)
        {
            _filters.Add(filter);
        }

        public override void Remove(Filter<T> filter)
        {
            _filters.Remove(filter);
        }
    }
}
