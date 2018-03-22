using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using ReplayParser.Loader;
using ReplayParser.Interfaces;

namespace ReplayParser.ReplaySorter
{
    public static class ReplayHandler
    {

        // put this stuff in a separate class? that way you can more easily keep track of data and reuse it 
        //public static void MoveReplay(Interfaces.IReplay replay, string filepath, string sortdirectory, string playername, bool rename, string format)
        //{
        //    var DirectoryName = Directory.GetParent(filepath);
        //    string FileName = filepath.Substring(DirectoryName.ToString().Length);
        //    if (rename == true)
        //    {
        //        try
        //        {
        //            CustomReplayFormat CustomFormat = new CustomReplayFormat(format);
        //            // parsecustomreplaynames
        //            // change filename based on format
        //            // you're splitting this again, and checking it again why? you've already done this...
        //            FileName = GenerateReplayName(replay, CustomFormat);
        //        }
        //        catch (ArgumentException Aex)
        //        {
        //            Console.WriteLine("Invalid custom format: {0}", Aex.Message);
        //        }

        //    }

        //    var DestinationFilePath = sortdirectory + @"\" + playername + FileName;
        //    File.Copy(filepath, DestinationFilePath);
        //}
        public static string MoveReplay(List<IReplay> ListReplays, IReplay replay, IEnumerable<string> Files, string sortDirectory, string FolderName, bool KeepOriginalReplayNames, CustomReplayFormat CustomReplayFormat)
        {
            var index = ListReplays.IndexOf(replay);
            var FilePath = Files.ElementAt(index);
            var DirectoryName = Directory.GetParent(FilePath);
            var FileName = FilePath.Substring(DirectoryName.ToString().Length);
            var DestinationFilePath = sortDirectory + @"\" + FolderName + FileName;

            if (!KeepOriginalReplayNames)
            {
                DestinationFilePath = sortDirectory + @"\" + FolderName + @"\" + GenerateReplayName(replay, CustomReplayFormat) + ".rep";
            }

            while (File.Exists(DestinationFilePath))
            {
                DestinationFilePath = Sorter.AdjustName(DestinationFilePath, false);
            }
            File.Move(FilePath, DestinationFilePath);
            return DestinationFilePath;
        }

        public static string CopyReplay(List<IReplay> ListReplays, IReplay replay, IEnumerable<string> Files, string sortDirectory, string FolderName, bool KeepOriginalReplayNames, CustomReplayFormat CustomReplayFormat)
        {
            var index = ListReplays.IndexOf(replay);
            var FilePath = Files.ElementAt(index);
            var DirectoryName = Directory.GetParent(FilePath);
            var FileName = FilePath.Substring(DirectoryName.ToString().Length);
            var DestinationFilePath = sortDirectory + @"\" + FolderName + FileName;

            if (!KeepOriginalReplayNames)
            {
                DestinationFilePath = sortDirectory + @"\" + FolderName + @"\" + GenerateReplayName(replay, CustomReplayFormat) + ".rep";
            }

            while (File.Exists(DestinationFilePath))
            {
                DestinationFilePath = Sorter.AdjustName(DestinationFilePath, false);
            }
            File.Copy(FilePath, DestinationFilePath);
            return DestinationFilePath;
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

        public static void LogBadReplays(List<string> ReplaysThrowingExceptions, string directory)
        {
            //var BadReplays = @"C:\testreplays\BadReplays";
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            try
            {
                using (var streamwriter = new StreamWriter(File.OpenWrite(directory + @"\BadReplays.txt")))
                {
                    foreach (var aBadReplay in ReplaysThrowingExceptions)
                    {
                        streamwriter.WriteLine(aBadReplay);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            //Console.WriteLine("Bad replays have been moved to: {0}", BadReplays);
        }

        // Enumerate subdirectories, find replay files, copy to 1 big folder??

        public static string GenerateReplayName(Interfaces.IReplay replay, CustomReplayFormat format)
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

            return CustomReplayName.ToString();

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

    }
}
