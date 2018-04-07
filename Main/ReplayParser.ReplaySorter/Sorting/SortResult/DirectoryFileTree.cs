using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace ReplayParser.ReplaySorter.Sorting.SortResult
{
    public class DirectoryFileTree
    {
        public DirectoryFileTree(DirectoryInfo self, List<string> files = null, List<DirectoryFileTree> children = null)
        {
            Self = self;
            Files = files;
            Children = children;
        }

        public DirectoryInfo Self { get; set; }
        public List<string> Files { get; set; }
        public List<DirectoryFileTree> Children { get; set; }

    }
}
