using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace ReplayParser.ReplaySorter.UserInput
{
    public class SearchDirectory
    {
        public SearchDirectory(string directory, SearchOption searchoption)
        {
            this.Directory = directory;
            this.SearchOption = searchoption;
        }
        public string Directory { get; set; }
        public SearchOption SearchOption { get; set; }
    }
}
