using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReplayParser.ReplaySorter.UI
{
    public class BoolAnswer
    {
        public BoolAnswer(string message, bool success)
        {
            Message = message;
            GoodToGo = success;
        }

        public string Message { get; set; }
        public bool GoodToGo { get; set; }
    }
}
