using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReplayParser.ReplaySorter
{
    public class File<T>
    {
        public File(string originalFileName)
        {
            OriginalFileName = originalFileName;
        }

        public T Content { get; set; }
        public string FileName { get; set; }
        public string OriginalFileName { get; }
    }
}
