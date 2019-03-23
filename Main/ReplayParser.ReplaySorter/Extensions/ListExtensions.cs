using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReplayParser.ReplaySorter.Extensions
{
    public static class ListExtensions
    {
        public static List<T> Union<T>(this List<T> list, List<T> other)
        {
            List<T> union = Activator.CreateInstance<List<T>>();
            var listSet = new HashSet<T>(list);

            foreach (var element in other)
            {
                if (listSet.Contains(element))
                    union.Add(element);
            }
            return union;
        }
    }
}
