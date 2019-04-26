using System;
using System.Collections.Generic;

namespace ReplayParser.ReplaySorter.Extensions
{
    public static class ListExtensions
    {
        public static List<T> Intersection<T>(this List<T> list, List<T> other)
        {
            List<T> intersection = Activator.CreateInstance<List<T>>();
            var listSet = new HashSet<T>(list);

            foreach (var element in other)
            {
                if (listSet.Contains(element))
                    intersection.Add(element);
            }
            return intersection;
        }
    }
}
