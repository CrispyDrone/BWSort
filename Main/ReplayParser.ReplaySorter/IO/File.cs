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
        public bool IsRenamed => _fileHistory.IsAtOriginal;
        public bool CanBeRenamed => !_fileHistory.IsAtLast;

        #endregion

        #region methods

        public void AddAfterCurrent(string fileName)
        {
            // verify filepath
            _fileHistory.AddAfterCurrent(fileName);
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

        #endregion

        #endregion

        //TODO add extension + filename + directory properties + tostring() override?
    }
}
