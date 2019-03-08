using System;

namespace ReplayParser.ReplaySorter
{
    public class ServiceResultSummary<T>
    {
        private int _operationErrorCount;
        private int _operationCount;
        private TimeSpan _duration;
        private string _message;
        private T _result;

        public ServiceResultSummary(T result, string message, TimeSpan duration, int operationCount, int errorCount)
        {
            _result = result;
            _message = message;
            _duration = duration;
            _operationCount = operationCount;
            _operationErrorCount = errorCount;
        }

        public T Result => _result;
        public string Message => _message;
        public TimeSpan Duration => _duration;
        public int OperationCount => _operationCount;
        public int ErrorCount => _operationErrorCount;
    }

    public class ServiceResultSummary : ServiceResultSummary<string>
    {
        public ServiceResultSummary
            (
                string result, 
                string message, 
                TimeSpan duration, 
                int operationCount, 
                int errorCount
            ) : base
            (
                result, 
                message, 
                duration, 
                operationCount, 
                errorCount
             )
        { }

        public static ServiceResultSummary Default => new ServiceResultSummary(string.Empty, string.Empty, TimeSpan.Zero, 0, 0);
    }
}
