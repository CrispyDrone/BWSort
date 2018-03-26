using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReplayParser.ReplaySorter.UserInput
{
    public class OutputDirectory : BaseDirectory
    {
        public OutputDirectory(string directory, string message) : base(directory)
        {
            Message = message;
        }

        public string Message { get; set; }
    }
}
