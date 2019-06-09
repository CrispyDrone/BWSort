using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ReplayParser.Loader;
using ReplayParser.Actions;
using System.IO;
using System.Globalization;
using System.Reflection;
using System.Diagnostics;
using ReplayParser.ReplaySorter.UserInput;
using ReplayParser.Interfaces;
using ReplayParser.ReplaySorter.Diagnostics;
using ReplayParser.ReplaySorter.Configuration;
using ReplayParser.ReplaySorter.IO;
using ReplayParser.ReplaySorter.Renaming;

namespace ReplayParser.ReplaySorter
{
    class Program
    {
        static void Main(string[] args)
        {
            // Try writing resource to disk C:\testreplays2 => success
            //if (!Directory.Exists(@"C:\testreplays2"))
            //{
            //    Directory.CreateDirectory(@"C:\testreplays2");
            //}
            //File.WriteAllBytes(@"C:\testreplays2\_000204_Fighting_Spirit_1_3_.rep", ReplayParser.ReplaySorter.Replays._000204_Fighting_Spirit_1_3_);

            // Try writing all resources to disk C:\testreplays2 => success
            //if (!Directory.Exists(@"C:\testreplays2"))
            //{
            //    Directory.CreateDirectory(@"C:\testreplays2");
            //}

            //var Resources = Assembly.GetExecutingAssembly().GetManifestResourceNames();
            //foreach (var aResource in Resources)
            //{
            //    if (aResource.Substring(aResource.Length - 4) == ".rep")
            //    {
            //        string FilePath = @"C:\testreplays2\" + aResource.Substring(36);
            //        using (Stream replayStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(aResource))
            //        {
            //            using (var FileStream = new FileStream(FilePath, FileMode.OpenOrCreate))
            //            {
            //                replayStream.CopyTo(FileStream);
            //            }
            //        }
            //    }
            //}

            // ask directory, if none supplied, check the embedded resources directory

            //if (directory == string.Empty)
            //{
            //    directory = Directory.GetCurrentDirectory() + @"\" + "Resources";
            //}

            // parse all replays in directory


            // for your UI, you should be able to put these things in a separate class and methods so you can make use of them instead of having to rewrite this...

            IReplaySorterConfiguration replaySorterConfiguration = new ReplaySorterAppConfiguration();
            if (ErrorLogger.GetInstance(replaySorterConfiguration) == null)
                Console.WriteLine("Issue intializing logger. Logging will be disabled.");

            var searchDirectory = (SearchDirectory)User.AskDirectory(typeof(SearchDirectory));
            List<File<IReplay>> ListReplays = new List<File<IReplay>>();
            IEnumerable<string> files = Directory.EnumerateFiles(searchDirectory.Directory, "*.rep", searchDirectory.SearchOption);

            while (files.Count() == 0)
            {
                Console.WriteLine("No replays found in {0}", searchDirectory.Directory);
                searchDirectory = (SearchDirectory)User.AskDirectory(typeof(SearchDirectory));
                files = Directory.EnumerateFiles(searchDirectory.Directory, "*.rep", searchDirectory.SearchOption);
            }
            // getting a lot of errors for replays, figure out why. Add possibility to rename the replays themselves.
            // Also figure out how to make this  1.16, 1.18, 1.19, ... compatible

            // could I put a try/catch inside a nother catch? Or should I just use a stringbuilder and write the entirety of it to a file in the end
            StringBuilder ErrorMessages = new StringBuilder();
            List<string> ReplaysThrowingExceptions = new List<string>();
            //var BadReplays = searchDirectory.Directory + @"\BadReplays";
            Stopwatch sw = new Stopwatch();
            sw.Start();
            var ProgressBar = new ProgressBar(Console.CursorLeft, Console.CursorTop);
            var numberOfFiles = files.Count();
            int currentPosition = 0;
            //try
            //{
            foreach (var replay in files)
            {
                currentPosition++;
                ProgressBar.ShowAndUpdate(currentPosition, numberOfFiles);
                try
                {
                    var ParsedReplay = ReplayLoader.LoadReplay(replay);
                    ListReplays.Add(File<IReplay>.Create(ParsedReplay, replay));
                    //WriteUncompressedReplay(@"C:\testreplays\UncompressedReplays", replay);
                }
                catch (Exception ex)
                {
                    //Console.WriteLine(ex.Message);
                    Console.WriteLine("\nError with replay {0}", replay.ToString());
                    ErrorMessages.AppendLine(ex.Message + ":" + replay.ToString());
                    ReplaysThrowingExceptions.Add(replay);
                }
            }
            Console.WriteLine();
            //}
            //catch(Exception ex)
            //{
            //    Console.WriteLine(ex.Message);
            //}
            sw.Stop();
            //var ErrorMessagesFile = @"C:\testreplays\ErrorMessages.txt";
            var ErrorMessagesFile = searchDirectory.Directory + @"\ErrorMessages.txt";
            Console.WriteLine("It took {0} seconds to parse {1} replays. {2} replays encountered exceptions during parsing. Errors can be investigated in the textfile: {3}", sw.Elapsed, ListReplays.Count(), ReplaysThrowingExceptions.Count(), ErrorMessagesFile);
            try
            {
                //using (var StreamWriter = new StreamWriter(ErrorMessagesFile))
                //{

                //}
                File.WriteAllText(ErrorMessagesFile, ErrorMessages.ToString());
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error writing error messages." + ex.Message);
            }

            if (ReplaysThrowingExceptions.Count() > 0)
            {
                Console.WriteLine("Move bad replays to a designated folder? Y(es)/N(o).");
                var MoveBadReplays = User.AskYesNo();
                if (MoveBadReplays.Yes != null)
                {
                    if ((bool)MoveBadReplays.Yes)
                    {
                        var BadReplaysOutputDirectory = (OutputDirectory)User.AskDirectory(typeof(OutputDirectory), "Specify a directory to put the bad replays.");
                        bool CreateDirectorySuccess = false;
                        while (!CreateDirectorySuccess)
                        {
                            try
                            {
                                Directory.CreateDirectory(BadReplaysOutputDirectory.Directory);
                                CreateDirectorySuccess = true;
                            }
                            catch (Exception)
                            {
                                Console.WriteLine("Could not create directory. Invalid directory name or not enough privileges.");
                                CreateDirectorySuccess = false;
                            }
                        }

                        foreach (var replay in ReplaysThrowingExceptions)
                        {
                            ReplayHandler.RemoveBadReplay(BadReplaysOutputDirectory.Directory + @"\BadReplays", replay);
                        }
                        ReplayHandler.LogBadReplays(ReplaysThrowingExceptions, replaySorterConfiguration.LogDirectory, $"{DateTime.Now} - Error while parsing replay: {{0}}");
                        
                    }
                }
                else
                {
                    Console.WriteLine("Answer can not be null");
                    return;
                }
                // remove bad replay from files
                // not necessary anymore since I use ReplayFile now
                // files = files.Where(x => !ReplaysThrowingExceptions.Contains(x));
            }

            // you might want to extract information and put everything into some sort of data structure, so you don't have to go through the list of replays for each
            // sort but instead can lookup in the datastructure/table? 


            //Ask criteria to sort on, let's start with something simple...
            //Player name

            // use a library to parse command line arguments => then return some sort of Enum
            // then pass this enum to a Sorter object
            OutputDirectory SortResultOutputDirectory = (OutputDirectory)User.AskDirectory(typeof(OutputDirectory), "Specify a directory for the sort result. If it does not exist, it will be created.");
            while (!Directory.Exists(SortResultOutputDirectory.Directory))
            {
                try
                {
                    Directory.CreateDirectory(SortResultOutputDirectory.Directory);
                }
                catch (Exception)
                {
                    Console.WriteLine("Could not create directory. Invalid directory name or not enough privileges.");
                    Console.WriteLine("Retry?");
                    var TryAgain = User.AskYesNo();
                    if (TryAgain.Yes != null)
                    {
                        if ((bool)TryAgain.Yes)
                        {
                            continue;
                        }
                        else
                        {
                            SortResultOutputDirectory = (OutputDirectory)User.AskDirectory(typeof(OutputDirectory), "Specify a different directory for the sort result.If it does not exist, it will be created.");
                        }
                    }
                    else
                    {
                        Console.WriteLine("Answer can not be null.");
                        return;
                    }
                }
            }

            Console.WriteLine("Please enter criteria to sort replays on.");
            Console.WriteLine("Provide a space separated list of criteria.");
            var SortCriteria = User.AskCriteria();
            Sorter sorter = new Sorter(SortResultOutputDirectory.Directory, ListReplays);
            List<string> replaysThrowingExceptions = new List<string>();
            while (SortCriteria.StopProgram != null)
            {
                if ((bool)!SortCriteria.StopProgram)
                {
                    var CriteriaParameters = User.AskAdditionalQuestionsSortCriteria(SortCriteria.ChosenCriteria);

                    Console.WriteLine("Keep original replay names?");
                    var KeepOriginalReplayNames = User.AskYesNo();
                    if (KeepOriginalReplayNames.Yes != null)
                    {
                        if ((bool)!KeepOriginalReplayNames.Yes)
                        {
                            CustomReplayFormat aCustomReplayFormat = null;
                            while (aCustomReplayFormat == null)
                            {
                                Console.WriteLine("Custom format?");
                                string customformat = Console.ReadLine();
                                try
                                {
                                    aCustomReplayFormat = CustomReplayFormat.Create(customformat, ListReplays.Count, true);
                                    sorter.CustomReplayFormat = aCustomReplayFormat;
                                }
                                catch (Exception)
                                {
                                    Console.WriteLine("Invalid custom format.");
                                }
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine("Answer can not be null");
                        return;
                    }

                    sorter.CurrentDirectory = SortResultOutputDirectory.Directory;
                    sorter.SortCriteria = SortCriteria.ChosenCriteria;
                    sorter.CriteriaStringOrder = SortCriteria.CriteriaStringOrder;
                    try
                    {
                        // use SortCriteriaParameters
                        sorter.ExecuteSort(CriteriaParameters.SortCriteriaParameters, (bool)KeepOriginalReplayNames.Yes, replaysThrowingExceptions);
                        Console.WriteLine("Sort finished.");
                        ReplayHandler.LogBadReplays(replaysThrowingExceptions, replaySorterConfiguration.LogDirectory, $"{DateTime.Now} - Error while sorting replay: {{0}} using arguments: {sorter.ToString()}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                    finally
                    {
                        Console.WriteLine("Please enter criteria to sort replays on.");
                        Console.WriteLine("Provide a space separated list of criteria.");
                        SortCriteria = User.AskCriteria();
                    }
                }
                else
                    return;

            }
            Console.WriteLine("Stop Program can not be null");
            return;

            //string consoleInput = Console.ReadLine();
            //Sorter sorter = new Sorter();
            //while (consoleInput.ToUpper() != "STOP")
            //{
            //    string[] Criteria = Array.ConvertAll(consoleInput.ToUpper().Split(new char[] { ' ' }), x => x.Trim());
            //    Criteria ChosenCriteria = new Criteria();
            //    foreach (var criteria in Criteria)
            //    {
            //        if (!Enum.TryParse(criteria, out ChosenCriteria))
            //        {
            //            Console.WriteLine("Invalid criteria: {0}", criteria);
            //        }
            //    }

            //    // again have sort of mapping structure to ask additional questions bsed on the criteria
            //    // for now keep it simple, just ask the questions
            //    Console.WriteLine("Keep original replay names?");
            //    var KeepOriginalReplayNames = User.AskYesNo();
            //    if (KeepOriginalReplayNames.Yes != null)
            //    {
            //        if ((bool)!KeepOriginalReplayNames.Yes)
            //        {
            //            CustomReplayFormat aCustomReplayFormat = new CustomReplayFormat();
            //            while (aCustomReplayFormat.CustomFormat == null)
            //            {
            //                Console.WriteLine("Custom format?");
            //                string customformat = Console.ReadLine();
            //                try
            //                {
            //                    aCustomReplayFormat.CustomFormat = customformat;
            //                    sorter.CustomReplayFormat = aCustomReplayFormat;
            //                }
            //                catch (Exception ex)
            //                {
            //                    Console.WriteLine(ex.Message);
            //                }
            //            }
            //        }
            //    }
            //    else
            //    {
            //        Console.WriteLine("Answer can not be null");
            //        return;
            //    }

            //    var CriteriaParameters = User.AskAdditionalQuestionsSortCriteria(ChosenCriteria);                

            //    sorter.CurrentDirectory = searchDirectory.Directory;
            //    sorter.Files = files;
            //    sorter.ListReplays = ListReplays;
            //    sorter.SortCriteria = ChosenCriteria;
            //    try
            //    {
            //        sorter.ExecuteSort(CriteriaParameters.MakeFolderForWinner, CriteriaParameters.MakeFolderForLoser, (bool)KeepOriginalReplayNames.Yes, CriteriaParameters.ValidGameTypes, CriteriaParameters.Durations);
            //        Console.WriteLine("Sort finished.");
            //    }
            //    catch (Exception ex)
            //    {
            //        Console.WriteLine(ex.Message);
            //    }
            //    finally
            //    {
            //        Console.WriteLine("Please enter criteria to sort replays on.");
            //        Console.WriteLine("Provide a space separated list of criteria.");
            //        consoleInput = Console.ReadLine();
            //    }
            //}


            // switch not going to work unless you go through each value between 1 to 2^x and not just the values defined in the enum
            //switch((int)ChosenCriteria)
            //{
            //    case 1:
            //        break;
            //    case 2:
            //        break;
            //    default:
            //        break;
            //}

            // use hasflag instead

            //if (ChosenCriteria.HasFlag(ReplaySorter.Criteria.PLAYERNAME))
            //{
            //    List<string> PlayerNames = new List<string>();
            //    // sort on playername
            //    foreach (var replay in ListReplays)
            //    {
            //        //List<string> Players = new List<string>();
            //        var parseplayers = replay.Players.ToList();
            //        foreach (var aplayer in parseplayers)
            //        {
            //            //Players.Add(aplayer.Name);
            //            if (!PlayerNames.Contains(aplayer.Name))
            //            {
            //                PlayerNames.Add(aplayer.Name);
            //            }
            //        }
            //            //PlayerNames.AddRange(Players);
            //    }

            //    // create sort directory, and directories for each player
            //    string sortDirectory = @directory + @"\" + ChosenCriteria.ToString();
            //    Directory.CreateDirectory(sortDirectory);
            //    foreach(var player in PlayerNames.Distinct())
            //    {
            //        Directory.CreateDirectory(sortDirectory + @"\" + player);
            //    }

            //    // now add all replays associated with player into the folder

            //foreach (var replay in ListReplays)
            //    {
            //        // get players per replay
            //        var ParsePlayers = replay.Players.ToList();
            //        var index = ListReplays.IndexOf(replay);
            //        var FilePath = files.ElementAt(index);
            //        var DirectoryName = Directory.GetParent(FilePath);
            //        var FileName = FilePath.Substring(DirectoryName.ToString().Length);

            //        foreach (var aPlayer in ParsePlayers)
            //        {
            //            // for each player, get proper folder
            //            // find the corresponding replay file
            //            // add this file to that folder
            //            var PlayerName = aPlayer.Name;
            //            var DestinationFilePath = sortDirectory + @"\" + PlayerName + FileName;
            //            File.Copy(FilePath, DestinationFilePath);
            //        }
            //    }
            //}
        }
    }
}
