using System.Collections.Generic;
using System.IO;
using ReplayParser.Interfaces;
using ReplayParser.ReplaySorter.IO;

namespace ReplayParser.ReplaySorter.Sorting.SortResult
{
    public class DirectoryFileTree<T> where T : class
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

    public class DirectoryFileTree : DirectoryFileTree<IReplay>
    {
        public DirectoryFileTree(DirectoryInfo self, List<File<IReplay>> files = null, List<DirectoryFileTree<IReplay>> children = null) : base(self, files, children) { }

    }
}
