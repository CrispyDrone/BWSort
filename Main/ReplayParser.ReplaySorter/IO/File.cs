using ReplayParser.Interfaces;
using System;
using System.Linq;

namespace ReplayParser.ReplaySorter.IO
{
    public class File<T> : IFile
    {
        #region private

        #region fields

        private FileHistory _fileHistory;
        private T _content;
        private string _hash;

        #endregion

        #region constructor

        protected File(T content, string originalFilePath, string hash)
        {
            _fileHistory = FileHistory.Create(originalFilePath);
            _content = content;
            _hash = hash;
        }

        #endregion

        #endregion

        #region public

        #region static constructor

        public static File<T> Create(T content, string originalFilePath, string hash = null)
        {
            if (content == null || string.IsNullOrWhiteSpace(originalFilePath)) return null;
            return new File<T>(content, originalFilePath, hash);
        }

        #endregion

        #region properties

        public string OriginalFilePath => _fileHistory.OriginalFilePath;
        public string FilePath => _fileHistory.CurrentFileName();
        public T Content => _content;
        public bool IsAtOriginal => _fileHistory.IsAtOriginal;
        public bool IsAtLast => _fileHistory.IsAtLast;
        public string Hash { get => _hash; set => _hash = value; }

        #endregion

        #region methods

        public void AddAfterCurrent(string fileName)
        {
            // verify filepath
            _fileHistory.AddAfterCurrent(fileName);
        }

        public void RemoveAfterCurrent()
        {
            _fileHistory.RemoveAfterCurrent();
        }

        public bool Rewind()
        {
            return _fileHistory.Rewind();
        }

        public bool Forward()
        {
            return _fileHistory.Forward();
        }

        public void ResetToOriginal()
        {
            _fileHistory.ResetToOriginal();
        }

        public void ResetToLast()
        {
            _fileHistory.ResetToLast();
        }

        public void SaveState()
        {
            _fileHistory.SaveState();
        }

        public void DiscardSavedState()
        {
            _fileHistory.DiscardSavedState();
        }

        public void RestoreToSavedState()
        {
            _fileHistory.RestoreSavedState();
        }

        public void CorrectCurrent(string fileName)
        {
            _fileHistory.CorrectCurrent(fileName);
        }

        #endregion

        #endregion

        //TODO add extension + filename + directory properties + tostring() override?
    }

    public class FileReplay : File<IReplay>
    {
        private FileReplay(IReplay content, string originalFilePath, string hash = null) : base(content, originalFilePath, hash) { }

        public new static FileReplay Create(IReplay content, string originalFilePath, string hash = null)
        {
            return new FileReplay(content, originalFilePath, hash);
        }

    }
}
