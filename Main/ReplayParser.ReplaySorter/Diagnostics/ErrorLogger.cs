using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace ReplayParser.ReplaySorter.Diagnostics
{
    public class ErrorLogger
    {
        private static ErrorLogger _errorLogger;
        private string _logPath;


        private static ErrorLogger Create(string logPath)
        {
            if (string.IsNullOrWhiteSpace(logPath)) return null;

            return new ErrorLogger(logPath);
        }

        private ErrorLogger(string logPath)
        {
            _logPath = logPath;
        }

        public static ErrorLogger GetInstance(string logPath = null)
        {
            if (_errorLogger == null)
            {
                _errorLogger = Create(logPath);
            }

            return _errorLogger;
        }

        public void LogError(string message, string filepath = "", Exception ex = null)
        {
            if (string.IsNullOrWhiteSpace(filepath)) filepath = _logPath;
            var now = DateTime.Now;

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
    }
}
