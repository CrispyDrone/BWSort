using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReplayParser.ReplaySorter.IO
{
    // derive from LinkedList<T>? Impossible because its methods are not marked virtual
    public class FileHistory
    {
        #region private

        #region fields

        private LinkedList<string> _fileNames = new LinkedList<string>();
        private LinkedListNode<string> _currentFileNameNode;

        #endregion

        #region methods

        private FileHistory(string originalFilePath)
        {
            _fileNames.AddFirst(originalFilePath);
            _currentFileNameNode = _fileNames.First;
        }

        #endregion

        #endregion

        #region public

        #region static constructor

        public static FileHistory Create(string originalFilePath)
        {
            if (string.IsNullOrWhiteSpace(originalFilePath)) return null;
            return new FileHistory(originalFilePath);
        }

        #endregion

        #region properties

        public string OriginalFilePath => _fileNames.First.Value;
        public bool IsAtOriginal => _currentFileNameNode == _fileNames.First;
        public bool IsAtLast => _currentFileNameNode == _fileNames.Last;

        #endregion

        #region methods

        /// <summary>
        /// Adds a new file name to the history chain after the current one and in the process delete any remaining filenames after it.
        /// </summary>
        /// <param name="fileName"></param>
        public void AddAfterCurrent(string fileName)
        {
            if (_currentFileNameNode.Next != null)
            {
                _fileNames.Remove(_currentFileNameNode.Next);
            }

            _fileNames.AddAfter(_currentFileNameNode, fileName);
        }

        public bool Rewind()
        {
            if (_currentFileNameNode.Previous == null)
                return false;

            _currentFileNameNode = _currentFileNameNode.Previous;
            return true;
        }

        public bool Forward()
        {
            if (_currentFileNameNode.Next == null)
                return false;

            _currentFileNameNode = _currentFileNameNode.Next;
            return true;
        }

        public string CurrentFileName()
        {
            return _currentFileNameNode.Value;
        }

        public void ResetToOriginal()
        {
            _currentFileNameNode = _fileNames.First;
        }

        public void ResetToLast()
        {
            _currentFileNameNode = _fileNames.Last;
        }

        #endregion

        #endregion
    }
}
