using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReplayParser.ReplaySorter.Diagnostics
{
    public class ProgressBar
    {
        public ProgressBar(int cursorleft, int cursortop)
        {
            cursorLeft = cursorleft;
            cursorTop = cursortop;
        }
        private int cursorLeft;
        private int cursorTop;
        public void ShowAndUpdate(int count, int totalCount)
        {
            // draw percentage of progress
            // redraw in same place
            decimal percentage = ((decimal)count / totalCount) * 100;
            Console.SetCursorPosition(cursorLeft, cursorTop);
            Console.Write("Parsing replays...\t[{0}{1}]", new string('*', (int)percentage), new string('.', (100 - (int)percentage)));
        }
    }
}
