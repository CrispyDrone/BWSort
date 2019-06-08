using System.Collections.Generic;
using System.Linq;

namespace ReplayParser.ReplaySorter
{
    public class ServiceResult<T>
    {
        private List<string> _errors;
        private bool _success;
        private T _result;

        public ServiceResult(T result, bool success, List<string> errors)
        {
            _result = result;
            _success = success;
            _errors = errors;
        }

        public T Result => _result;
        public bool Success => _success;
        public IEnumerable<string> Errors => _errors.AsEnumerable();
    }

    public class ServiceResult : ServiceResult<string>
    {
        public ServiceResult(string result, bool success, List<string> errors) : base(result, success, errors)
        {
        }

    }
}
