using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReplayParser.ReplaySorter
{
    public class File<T>
    {
        public T Content { get; set; }
        public string FileName { get; set; }
    }
}
