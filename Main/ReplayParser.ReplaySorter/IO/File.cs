using System.Linq;

namespace ReplayParser.ReplaySorter.IO
{
    public class File<T>
    {
        #region private

        #region fields

        private FileHistory _fileHistory;
        private T _content;

        #endregion

        #region constructor

        private File(T content, string originalFilePath)
        {
            _fileHistory = FileHistory.Create(originalFilePath);
            _content = content;
        }

        #endregion

        #endregion

        #region public

        #region static constructor

        public static File<T> Create(T content, string originalFilePath)
        {
            if (content == null || string.IsNullOrWhiteSpace(originalFilePath)) return null;
            return new File<T>(content, originalFilePath);
        }

        #endregion

        #region properties

        public string OriginalFilePath => _fileHistory.OriginalFilePath;
        public string FilePath => _fileHistory.CurrentFileName();
        public T Content => _content;
        public bool IsAtOriginal => _fileHistory.IsAtOriginal;
        public bool IsAtLast => _fileHistory.IsAtLast;

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
}
