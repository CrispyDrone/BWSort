using System.Collections;
using System.Collections.Generic;
using System.IO;
using ReplayParser.Interfaces;
using ReplayParser.ReplaySorter.IO;
using System.Linq;

namespace ReplayParser.ReplaySorter.Sorting.SortResult
{
    public class DirectoryFileTree<T> : IEnumerable<DirectoryFileTree<T>> where T : class
    {
        public DirectoryFileTree(DirectoryInfo self, List<T> files = null, List<DirectoryFileTree<T>> children = null)
        {
            Self = self;
            Files = files;
            Children = children;
        }

        public DirectoryInfo Self { get; set; }
        public List<T> Files { get; set; }
        public List<DirectoryFileTree<T>> Children { get; set; }

        public IEnumerator<DirectoryFileTree<T>> GetEnumerator()
        {
            return Children.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    public class DirectoryFileTree : DirectoryFileTree<FileReplay>
    {
        public DirectoryFileTree(DirectoryInfo self, List<FileReplay> files = null, List<DirectoryFileTree> children = null) : base(self, files, children?.Cast<DirectoryFileTree<FileReplay>>().ToList()) { }

    }
}
