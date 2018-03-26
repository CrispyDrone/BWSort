using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace ReplayParser.ReplaySorter.UserInput
{
    public class SearchDirectory : BaseDirectory
    {
        public SearchDirectory(string directory, SearchOption searchoption) : base(directory)
        {
            this.SearchOption = searchoption;
        }
        public SearchOption SearchOption { get; set; }
    }
}
