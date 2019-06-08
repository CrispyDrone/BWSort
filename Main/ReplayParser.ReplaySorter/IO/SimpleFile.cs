using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReplayParser.ReplaySorter.IO
{
    public class SimpleFile : IFile
    {
        public SimpleFile(string filePath)
        {
            FilePath = filePath;
        }

        public string FilePath { get; }
    }
}
