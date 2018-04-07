using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReplayParser.ReplaySorter.UI
{
    public class BoolAnswer
    {
        public BoolAnswer(string message, bool error)
        {
            Message = message;
            GoodToGo = error;
        }

        public string Message { get; set; }
        public bool GoodToGo { get; set; }
    }
}
