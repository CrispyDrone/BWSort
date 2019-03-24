using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace ReplayParser.ReplaySorter.Filtering
{
    /// <summary>
    /// An filter for a set of elements.
    /// </summary>
    public abstract class Filter<T>
    {
        public abstract IQueryable<T> Execute(IList<T> list);
        public abstract void Add(Filter<T> filter);
        public abstract void Remove(Filter<T> filter);
        public virtual bool IsComplex => false;
    }
}
