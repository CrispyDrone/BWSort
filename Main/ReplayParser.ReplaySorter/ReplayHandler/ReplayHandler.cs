using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using ReplayParser.Loader;
using ReplayParser.Interfaces;
using ReplayParser.ReplaySorter.IO;
using ReplayParser.ReplaySorter.Diagnostics;
using System.Text.RegularExpressions;

namespace ReplayParser.ReplaySorter
{
    public static class ReplayHandler
    {
        public static void MoveReplay(File<IReplay> replay, string sortDirectory, string FolderName, bool KeepOriginalReplayNames, CustomReplayFormat CustomReplayFormat, bool isPreview = false)
        {
            var sourceFilePath = replay.FilePath;
            var FileName = FileHandler.GetFileName(replay.FilePath);
            var DestinationFilePath = sortDirectory + @"\" + FolderName + @"\" + FileName;

            if (!KeepOriginalReplayNames)
            {
                try
                {
                    DestinationFilePath = sortDirectory + @"\" + FolderName + @"\" + GenerateReplayName(replay.Content, CustomReplayFormat) + ".rep";
                }
                catch(Exception ex)
                {
                    ErrorLogger.GetInstance()?.LogError($"{DateTime.Now} - Error while renaming replay: {replay.OriginalFilePath}", ex : ex);
                }
            }

            DestinationFilePath = FileHandler.AdjustName(DestinationFilePath, false);

            if (!isPreview)
            {
                File.Move(sourceFilePath, DestinationFilePath);
            }

            replay.AddAfterCurrent(DestinationFilePath);
            replay.Forward();
        }

        //TODO accept 2 filepaths
        public static void MoveReplay(File<IReplay> replay, bool forward = true)
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

            File.Move(filePath, destinationFilePath);
        }

        public static void CopyReplay(File<IReplay> replay, string sortDirectory, string FolderName, bool KeepOriginalReplayNames, CustomReplayFormat CustomReplayFormat, bool isPreview = false)
        {
            var sourceFilePath = replay.FilePath;
            var FileName = FileHandler.GetFileName(replay.FilePath);
            var DestinationFilePath = sortDirectory + @"\" + FolderName + @"\" + FileName;

            if (!KeepOriginalReplayNames)
            {
                try
                {
                    DestinationFilePath = sortDirectory + @"\" + FolderName + @"\" + GenerateReplayName(replay.Content, CustomReplayFormat) + ".rep";
                }
                catch (Exception ex)
                {
                    ErrorLogger.GetInstance()?.LogError($"{DateTime.Now} - Error while renaming replay: {replay.OriginalFilePath}", ex : ex);
                }
            }

            DestinationFilePath = FileHandler.AdjustName(DestinationFilePath, false);

            if (!isPreview)
            {
                File.Copy(sourceFilePath, DestinationFilePath);
            }
            replay.AddAfterCurrent(DestinationFilePath);
            replay.Forward();
        }

        //TODO accept 2 filepaths
        public static void CopyReplay(File<IReplay> replay, bool forward = true)
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

            File.Copy(filePath, destinationFilePath);
        }

        public static void RemoveBadReplay(string filepath, string abadreplay)
        {
            if (!Directory.Exists(filepath))
            {
                Directory.CreateDirectory(filepath);
            }
            File.Move(abadreplay, filepath + @"\" + abadreplay.Substring(abadreplay.LastIndexOf('\\') + 1));
        }

        public static void WriteUncompressedReplay(string filepath, string replay)
        {
            if (!Directory.Exists(filepath))
            {
                Directory.CreateDirectory(filepath);
            }
            var unpackedreplay = ReplayLoader.LoadReplay(new BinaryReader(new FileStream(replay, FileMode.Open)), true);

            try
            {
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
                Console.WriteLine(ex.Message);
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
            {
                Directory.CreateDirectory(directory);
            }

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

            //Console.WriteLine("Bad replays have been moved to: {0}", BadReplays);
        }

        // Enumerate subdirectories, find replay files, copy to 1 big folder??

        public static string GenerateReplayName(IReplay replay, CustomReplayFormat format)
        {
            // generate mapping structure
            // make interface that has method "GenerateReplayNameSection"
            // make base class that implements interface
            // derive a class per CustomReplayNameSyntax from this base class, fill in the code in the method


            Dictionary<CustomReplayNameSyntax, string> CustomReplayNameSections = format.GenerateReplayNameSections(replay);

            StringBuilder CustomReplayName = new StringBuilder();

            string[] arguments = format.CustomFormat.Split(new char[] { '|' });

            for (int i = 0; i < arguments.Length; i++)
            {
                CustomReplayName.Append(CustomReplayNameSections[(CustomReplayNameSyntax)Enum.Parse(typeof(CustomReplayNameSyntax), arguments[i])]);
            }


            // remove invalid characters from the name

            string CustomReplayNameString = RemoveInvalidChars(CustomReplayName.ToString());

            return CustomReplayNameString;

            //Dictionary<int, Dictionary<CustomReplayNameSyntax, string[]>> CustomReplayNameSectionsForAllTeams = new Dictionary<int, Dictionary<CustomReplayNameSyntax, string[]>>();

            //int NumberOfTeams = CustomReplayNameSectionsForAllTeams.Count;

            //StringBuilder CustomReplayName = new StringBuilder();

            //Dictionary<int, string> ArgumentsDictionary = new Dictionary<int, string>();

            //string[] arguments = format.CustomFormat.Split(new char[] { '|' });
            //// now create the argumentsdictionary that consists of the arguments with an index denoting the team number it belongs to
            //foreach (var argument in arguments)
            //{
            //    arguments.Count(x => x == argument)
            //}
        }

        public static char[] InvalidFileChars = Path.GetInvalidFileNameChars();
        public static char[] InvalidPathChars = Path.GetInvalidPathChars();
        //public static char[] InvalidFileCharsAdditional = new char[] { '*', ':' };

        //TODO use StringBuilder instead??
        public static string RemoveInvalidChars(string name)
        {
            foreach (var InvalidChar in InvalidPathChars)
            {
                name = name.Replace(InvalidChar.ToString(), string.Empty);
            }
            foreach (var InvalidChar in InvalidFileChars)
            {
                name = name.Replace(InvalidChar.ToString(), string.Empty);
            }
            //foreach (var InvalidChar in InvalidFileCharsAdditional)
            //{
            //    name = name.Replace(InvalidChar.ToString(), string.Empty);
            //}
            return name;
        }

        public static void SaveReplayFilePaths(List<File<IReplay>> listReplays)
        {
            if (listReplays == null)
                return;

            foreach (var replay in listReplays)
            {
                replay.SaveState();
            }
        }

        public static void ResetReplayFilePathsToBeforeSort(List<File<IReplay>> listReplays)
        {
            if (listReplays == null)
                return;

            foreach (var replay in listReplays)
            {
                replay.RestoreToSavedState();
                replay.RemoveAfterCurrent();
            }
        }
    }
}
