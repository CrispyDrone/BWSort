using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReplayParser.ReplaySorter.UserInput
{
    public class BaseDirectory
    {
        public BaseDirectory(string directory)
        {
            Directory = directory;
        }
        public string Directory { get; set; }
    }
}
