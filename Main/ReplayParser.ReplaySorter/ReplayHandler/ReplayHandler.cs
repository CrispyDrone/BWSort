using ReplayParser.Interfaces;
using ReplayParser.Loader;
using ReplayParser.ReplaySorter.Diagnostics;
using ReplayParser.ReplaySorter.IO;
using ReplayParser.ReplaySorter.Renaming;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System;
using ReplayParser.ReplaySorter.Exceptions;

namespace ReplayParser.ReplaySorter
{
    public static class ReplayHandler
    {
        // public static void MoveReplay(File<IReplay> replay, string sortDirectory, string FolderName, bool KeepOriginalReplayNames, CustomReplayFormat CustomReplayFormat, bool isPreview = false)
        // {
        //     var sourceFilePath = replay.FilePath;
        //     var FileName = FileHandler.GetFileName(replay.FilePath);
        //     var DestinationFilePath = sortDirectory + @"\" + FolderName + @"\" + FileName;

        //     if (!KeepOriginalReplayNames)
        //     {
        //         try
        //         {
        //             DestinationFilePath = sortDirectory + @"\" + FolderName + @"\" + GenerateReplayName(replay, CustomReplayFormat) + ".rep";
        //         }
        //         catch(Exception ex)
        //         {
        //             ErrorLogger.GetInstance()?.LogError($"{DateTime.Now} - Error while renaming replay: {replay.OriginalFilePath}", ex : ex);
        //         }
        //     }

        //     DestinationFilePath = FileHandler.AdjustName(DestinationFilePath, false);

        //     if (!isPreview)
        //     {
        //         File.Move(sourceFilePath, DestinationFilePath);
        //     }

        //     replay.AddAfterCurrent(DestinationFilePath);
        //     replay.Forward();
        // }

        public static void MoveReplay(File<IReplay> replay, string sortDirectory, string folderName, bool keepOriginalReplayNames, CustomReplayFormat customReplayFormat, bool isPreview = false)
        {
            if (replay == null) throw new ArgumentNullException(nameof(replay));
            if (string.IsNullOrWhiteSpace(sortDirectory)) throw new ArgumentException(nameof(sortDirectory));
            if (!keepOriginalReplayNames && customReplayFormat == null) throw new InvalidOperationException($"{nameof(customReplayFormat)} cannot be null if {nameof(keepOriginalReplayNames)} is false!");

            var sourceFilePath = replay.FilePath;
            var fileName = FileHandler.GetFileName(replay.FilePath);
            var destinationFilePath = Path.Combine(sortDirectory, folderName, fileName);
            var renamedSuccessfully = true;
            Exception renameException = null;

            if (!keepOriginalReplayNames)
            {
                try
                {
                    destinationFilePath = sortDirectory + @"\" + folderName + @"\" + GenerateReplayName(replay, customReplayFormat) + ".rep";
                }
                catch(Exception ex)
                {
                    renamedSuccessfully = false;
                    renameException = ex;
                    ErrorLogger.GetInstance()?.LogError($"{DateTime.Now} - Error while renaming replay: {replay.OriginalFilePath}", ex : ex);
                }
            }

            destinationFilePath = FileHandler.AdjustName(destinationFilePath, false);

            if (!isPreview)
                File.Move(sourceFilePath, destinationFilePath);

            replay.AddAfterCurrent(destinationFilePath);
            replay.Forward();

            if (!renamedSuccessfully)
                throw new RenameException(sourceFilePath, customReplayFormat, $"Something went wrong while renaming.", renameException);
        }

        //TODO accept 2 filepaths
        public static void MoveReplay(File<IReplay> replay, bool forward = true/*, bool isPreview = false*/)
        {
            var filePath = replay.FilePath;

            if (forward)
            {
                if (!replay.Forward())
                    return;
            }
            else
            {
                if (!replay.Rewind())
                    return;

            }

            var destinationFilePath = replay.FilePath;
            if (destinationFilePath != filePath)
            {
                destinationFilePath = FileHandler.AdjustName(destinationFilePath, false);
                replay.CorrectCurrent(destinationFilePath);
            }
            //TODO this should belong in the File(history) class when accepting a new file, not possible because you can't verify whether there are doubles
            // instead sorting/renaming should be virtual with virtual directories that are aware of other replays in the directory...

            //if (!isPreview)
            //{
                File.Move(filePath, destinationFilePath);
            //}
        }

        public static void CopyReplay(File<IReplay> replay, string sortDirectory, string folderName, bool keepOriginalReplayNames, CustomReplayFormat customReplayFormat, bool isPreview = false)
        {
            if (replay == null) throw new ArgumentNullException(nameof(replay));
            if (string.IsNullOrWhiteSpace(sortDirectory)) throw new ArgumentException(nameof(sortDirectory));
            if (!keepOriginalReplayNames && customReplayFormat == null) throw new InvalidOperationException($"{nameof(customReplayFormat)} cannot be null if {nameof(keepOriginalReplayNames)} is false!");

            var sourceFilePath = replay.FilePath;
            var fileName = FileHandler.GetFileName(replay.FilePath);
            var destionationFilePath = Path.Combine(sortDirectory, folderName, fileName);
            var renamedSuccessfully = true;
            Exception renameException = null;

            if (!keepOriginalReplayNames)
            {
                try
                {
                    destionationFilePath = sortDirectory + @"\" + folderName + @"\" + GenerateReplayName(replay, customReplayFormat) + ".rep";
                }
                catch (Exception ex)
                {
                    renamedSuccessfully = false;
                    renameException = ex;
                    ErrorLogger.GetInstance()?.LogError($"{DateTime.Now} - Error while renaming replay: {replay.OriginalFilePath}", ex : ex);
                }
            }

            destionationFilePath = FileHandler.AdjustName(destionationFilePath, false);

            if (!isPreview)
                File.Copy(sourceFilePath, destionationFilePath);

            replay.AddAfterCurrent(destionationFilePath);
            replay.Forward();
            
            if (!renamedSuccessfully)
                throw new RenameException(sourceFilePath, customReplayFormat, $"Something went wrong while renaming.", renameException);
        }

        //TODO accept 2 filepaths
        public static void CopyReplay(File<IReplay> replay, bool forward = true/*, bool isPreview = false*/)
        {
            var filePath = replay.FilePath;

            if (forward)
            {
                if (!replay.Forward())
                    return;
            }
            else
            {
                if (!replay.Rewind())
                    return;
            }

            var destinationFilePath = replay.FilePath;
            //TODO this should belong in the File(history) class when accepting a new file
            destinationFilePath = FileHandler.AdjustName(destinationFilePath, false);
            replay.CorrectCurrent(destinationFilePath);

            //if (!isPreview)
            //{
                File.Copy(filePath, destinationFilePath);
            //}
        }

        public static void RemoveBadReplay(string filepath, string abadreplay)
        {
            if (!Directory.Exists(filepath))
                Directory.CreateDirectory(filepath);

            File.Move(abadreplay, filepath + @"\" + abadreplay.Substring(abadreplay.LastIndexOf('\\') + 1));
        }

        public static void WriteUncompressedReplay(string filepath, string replay)
        {
            if (!Directory.Exists(filepath))
                Directory.CreateDirectory(filepath);

            try
            {
                var unpackedreplay = ReplayLoader.LoadReplay(new BinaryReader(new FileStream(replay, FileMode.Open)), true);
                using (var binarywriter = new BinaryWriter(File.OpenWrite(filepath + @"\" + replay.Substring(replay.LastIndexOf('\\') + 1))))
                {
                    binarywriter.Write(unpackedreplay.Identifier);
                    binarywriter.Write("Header");
                    binarywriter.Write(unpackedreplay.Header);
                    binarywriter.Write("Actions");
                    binarywriter.Write(unpackedreplay.Actions);
                    binarywriter.Write("Map");
                    binarywriter.Write(unpackedreplay.Map);
                    Console.WriteLine("Finished writing unpacked replay {0}.", replay);
                }
            }
            catch (Exception ex)
            {
                ErrorLogger.GetInstance()?.LogError($"{DateTime.Now} - Something went wrong while writing the uncompressed replay: {ex.Message}", ex: ex);
            }
        }

        private static readonly string _stringFormatArgument = @"\{0\}";
        private static Regex StringFormatArgument = new Regex(_stringFormatArgument);

        public static void LogBadReplays(List<string> ReplaysThrowingExceptions, string directory, string formatExpression = "{0}", string header = "", string footer = "")
        {
            if (!StringFormatArgument.IsMatch(formatExpression))
                return;

            //var BadReplays = @"C:\testreplays\BadReplays";
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            using (var streamwriter = new StreamWriter(File.OpenWrite(directory + @"\BadReplays.txt")))
            {
                if (!string.IsNullOrWhiteSpace(header))
                    streamwriter.WriteLine(header);

                foreach (var aBadReplay in ReplaysThrowingExceptions)
                {
                    try
                    {
                        streamwriter.WriteLine(string.Format(formatExpression, aBadReplay));
                    }
                    catch (Exception ex)
                    {
                        ErrorLogger.GetInstance()?.LogError($"{DateTime.Now} - Error while logging bad replay: {aBadReplay}", ex: ex);
                    }
                }

                if (!string.IsNullOrWhiteSpace(footer))
                    streamwriter.WriteLine(footer);
            }

        }

        /// <summary>
        /// Generates a new replay name based on a replay and a custom replay format.
        /// </summary>
        /// <param name="replay">The replay to generate a new name for.</param>
        /// <param name="format">The custom replay format to use to generate a new replay name.</param>
        /// <returns></returns>
        public static string GenerateReplayName(File<IReplay> replay, CustomReplayFormat format)
        {
            if (replay == null) throw new ArgumentNullException(nameof(replay));
            if (format == null) throw new ArgumentNullException(nameof(format));

            var customReplayName = format.GenerateReplayName(replay);
            return FileHandler.RemoveInvalidChars(customReplayName);
        }

        /// <summary>
        /// Renames a replay according to a custom replay format.
        /// </summary>
        /// <param name="replay">The replay to rename.</param>
        /// <param name="format">The custom replay format to use to rename the replay.</param>
        /// <param name="forward">Whether to move the file history pointer forward or not.</param>
        /// <exception cref="ArgumentNullException">Throws an ArgumentNullException in case replay or format is null.</exception>
        public static void RenameReplay(File<IReplay> replay, CustomReplayFormat format, bool forward = true)
        {
            var newName = GenerateReplayName(replay, format);
            replay.AddAfterCurrent(newName);
            if (forward)
            {
                replay.Forward();
            }
        }

        /// <summary>
        /// Saves the current head pointer for all the provided replays.
        /// </summary>
        /// <param name="listReplays">The set of replays to operate on.</param>
        public static void SaveReplayFilePaths(List<File<IReplay>> listReplays)
        {
            if (listReplays == null)
                return;

            foreach (var replay in listReplays)
            {
                replay.SaveState();
            }
        }

        /// <summary>
        /// Restores the provided replays to a saved state. And removes the file history after the new head pointer.
        /// </summary>
        /// <param name="listReplays">The set of replays to operate on.</param>
        /// <param name="throwOnFailure">If true, will throw if a single replay fails to be restored.</param>
        /// <exception cref="InvalidOperationException">Throws invalid operation exception in case a replay does not contain a saved state.</exception>
        public static void RestoreToSavedStateAndClearFuture(List<File<IReplay>> listReplays, bool throwOnFailure = false)
        {
            if (listReplays == null)
                return;

            foreach (var replay in listReplays)
            {
                try
                {
                    replay.RestoreToSavedState();
                    replay.RemoveAfterCurrent();
                }
                catch (InvalidOperationException ex)
                {
                    ErrorLogger.GetInstance()?.LogError($"{DateTime.Now} - Something went wrong while restoring to a saved state.", ex: ex);
                    if (throwOnFailure)
                        throw;
                }
            }
        }

        internal static void MoveReplay(File<IReplay> replay, string newReplayPath)
        {
            throw new NotImplementedException();
        }

        internal static void CopyReplay(File<IReplay> replay, string newReplayPath)
        {
            throw new NotImplementedException();
        }
    }
}
