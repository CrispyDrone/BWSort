using ReplayParser.ReplaySorter.Renaming;
using System;

namespace ReplayParser.ReplaySorter.Exceptions
{
    public class RenameException : Exception
    {
        public RenameException(string originalFilePath, string message) : base(message)
        {
            OriginalFilePath = originalFilePath;
        }

        public RenameException(string originalFilePath, CustomReplayFormat customReplayFormat, string message) : base(message)
        {
            OriginalFilePath = originalFilePath;
            CustomReplayFormat = customReplayFormat;
        }

        public RenameException(string originalFilePath, CustomReplayFormat customReplayFormat, string message, Exception innerException) : base(message, innerException)
        {
            OriginalFilePath = originalFilePath;
            CustomReplayFormat = customReplayFormat;
        }

        public RenameException(string message, Exception innerException) : base(message, innerException) { }

        public string OriginalFilePath { get; }
        public CustomReplayFormat CustomReplayFormat { get; }
    }
}
