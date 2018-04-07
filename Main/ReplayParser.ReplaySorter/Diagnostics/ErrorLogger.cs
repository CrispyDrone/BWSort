using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace ReplayParser.ReplaySorter.Diagnostics
{
    public static class ErrorLogger
    {
        public static void LogError(string message, string filepath, Exception ex)
        {
            using (var StreamWriter = new StreamWriter(filepath, true))
            {
                StreamWriter.WriteLine("Custom message: {0}", message);
                StreamWriter.WriteLine("Exception message: {0}", ex.Message);
                StreamWriter.WriteLine("Stacktrace:{0}", ex.StackTrace);
                StreamWriter.WriteLine(new string('=', 20));
            }
        }
    }
}
