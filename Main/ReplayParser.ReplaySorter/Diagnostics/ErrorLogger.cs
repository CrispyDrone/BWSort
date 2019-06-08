using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using ReplayParser.ReplaySorter.Configuration;

namespace ReplayParser.ReplaySorter.Diagnostics
{
    public class ErrorLogger
    {
        private static ErrorLogger _errorLogger;
        private string _logPath;

        private static ErrorLogger Create(string logDirectory)
        {
            if (string.IsNullOrWhiteSpace(logDirectory)) return null;

            return new ErrorLogger(logDirectory);
        }

        private ErrorLogger(string logDirectory)
        {
            _logPath = Path.Combine(logDirectory, "ErrorLogs.txt");
        }

        public static ErrorLogger GetInstance(IReplaySorterConfiguration replaySorterConfiguration = null)
        {
            if (_errorLogger == null)
            {
                if (replaySorterConfiguration == null)
                    replaySorterConfiguration = new ReplaySorterAppConfiguration();

                _errorLogger = Create(replaySorterConfiguration.LogDirectory);
            }

            return _errorLogger;
        }

        public void LogError(string message, string filepath = "", Exception ex = null)
        {
            if (string.IsNullOrWhiteSpace(filepath)) filepath = _logPath;
            var now = DateTime.Now;

            try
            {
                using (var StreamWriter = new StreamWriter(filepath, true))
                {
                    StreamWriter.WriteLine("{0} - Custom message: {1}", now, message);
                    if (ex != null)
                    {
                        StreamWriter.WriteLine("{0} Exception message: {1}", now, ex.Message);
                        StreamWriter.WriteLine("{0} Stacktrace: {1}", now, ex.StackTrace);
                    }
                    StreamWriter.WriteLine(new string('=', 20));
                }
            }
            catch { }
        }
    }
}
