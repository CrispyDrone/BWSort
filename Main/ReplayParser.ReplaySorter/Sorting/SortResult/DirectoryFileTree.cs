using System.Collections.Generic;
using System.IO;
using ReplayParser.ReplaySorter.IO;

namespace ReplayParser.ReplaySorter.Sorting.SortResult
{
    public class DirectoryFileTree<T>
    {
        public DirectoryFileTree(DirectoryInfo self, List<File<T>> files = null, List<DirectoryFileTree<T>> children = null)
        {
            Self = self;
            Files = files;
            Children = children;
        }

        public DirectoryInfo Self { get; set; }
        public List<File<T>> Files { get; set; }
        public List<DirectoryFileTree<T>> Children { get; set; }

    }
}
