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
        private LinkedListNode<string> _savedFileNameNode;

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
            RemoveAfterCurrent();
            _fileNames.AddAfter(_currentFileNameNode, fileName);
        }

        public void RemoveAfterCurrent()
        {
            var currentNode = _currentFileNameNode;

            //TODO maybe i should make my own linkedlist implementation so i can just cut a linkedlist at a specified node... since this will be horribly quite inefficient with long histories..
            while (currentNode.Next != null)
            {
                if (_savedFileNameNode == currentNode.Next)
                    _savedFileNameNode = null;

                _fileNames.Remove(currentNode.Next);
            }
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

        public void SaveState()
        {
            if (_currentFileNameNode == null)
                throw new NullReferenceException("Current file path is null!");

            _savedFileNameNode = _currentFileNameNode;
        }

        public void RestoreSavedState()
        {
            if (_savedFileNameNode == null)
                throw new InvalidOperationException("Attempting to restore state, but it has been discarded!");

            _currentFileNameNode = _savedFileNameNode;
        }

        public void CorrectCurrent(string fileName)
        {
            if (_currentFileNameNode == null)
                throw new NullReferenceException("Current file path is null!");

            if (_currentFileNameNode == _fileNames.First)
                return;
                // throw new InvalidOperationException("Attempting to modify original path!");

            _currentFileNameNode.Value = fileName;
        }

        #endregion

        #endregion
    }
}
