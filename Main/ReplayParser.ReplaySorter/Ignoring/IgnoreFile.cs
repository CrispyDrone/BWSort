using System;
using System.Collections.Generic;
using System.Linq;

namespace ReplayParser.ReplaySorter.Ignoring
{
    [Serializable]
    public class IgnoreFile
    {
        #region private

        #region fields

        private List<Tuple<string, string>> _ignoredFiles = new List<Tuple<string, string>>();
        [NonSerialized]
        private IgnoreFileEqualityComparer _eqComparer = new IgnoreFileEqualityComparer();

        #endregion

        #endregion

        #region public

        #region properties

        public IEnumerable<Tuple<string, string>> IgnoredFiles => _ignoredFiles.AsEnumerable();

        #endregion

        #region methods

        public void Ignore(Tuple<string, string> file)
        {
            if (_ignoredFiles.Contains(file, _eqComparer))
                return;

            _ignoredFiles.Add(file);
        }

        public void UnIgnore(string hash)
        {
            if (string.IsNullOrEmpty(hash))
                return;

            var toRemove = _ignoredFiles.FirstOrDefault(f => f.Item2 == hash);
            if (toRemove == null)
                return;

            _ignoredFiles.Remove(toRemove);
        }

        public void Clear()
        {
            _ignoredFiles.Clear();
        }

        [Serializable]
        private class IgnoreFileEqualityComparer : IEqualityComparer<Tuple<string, string>>
        {
            public bool Equals(Tuple<string, string> x, Tuple<string, string> y)
            {
                if (x == null || y == null)
                    return false;

                if (x.Item2 == y.Item2)
                    return true;

                return false;
            }

            public int GetHashCode(Tuple<string, string> obj)
            {
                return obj.Item2.GetHashCode();
            }
        }

        #endregion

        #endregion
    }
}
