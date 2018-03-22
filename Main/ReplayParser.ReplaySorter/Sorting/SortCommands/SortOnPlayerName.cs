using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ReplayParser.ReplaySorter.Sorting;
using System.IO;
using ReplayParser.Interfaces;

namespace ReplayParser.ReplaySorter.Sorting.SortCommands
{
    public class SortOnPlayerName : ISortCommand
    {
        public SortOnPlayerName(SortCriteriaParameters sortcriteriaparameters, bool keeporiginalreplaynames, Sorter sorter)
        {
            SortCriteriaParameters = sortcriteriaparameters;
            KeepOriginalReplayNames = keeporiginalreplaynames;
            Sorter = sorter;
        }
        public bool KeepOriginalReplayNames { get; set; }
        public SortCriteriaParameters SortCriteriaParameters { get; set; }
        public Sorter Sorter { get; set; }
        public Criteria SortCriteria { get { return Criteria.PLAYERNAME; } }
        public bool IsNested { get; set; }
        public IDictionary<string, IDictionary<string, IReplay>> Sort()
        {
            // Dictionary<directory, dictionary<file, replay>>
            IDictionary<string, IDictionary<string, IReplay>> DirectoryFileReplay = new Dictionary<string, IDictionary<string, IReplay>>();

            List<string> PlayerNames = new List<string>();
            List<string> WinnersAndLosers = new List<string>();
            List<string> Winners = new List<string>();
            List<string> Losers = new List<string>();
            bool MakeFolderForWinner = (bool)SortCriteriaParameters.MakeFolderForWinner;
            bool MakeFolderForLoser = (bool)SortCriteriaParameters.MakeFolderForLoser;
            string CurrentDirectory = Sorter.CurrentDirectory;
            Criteria SortCriteria = Sorter.SortCriteria;

            // Rewrite to PlayerNames.AddRange(ExtractPlayers(TypeOfPlayer, ListOfPlayer)....
            if (MakeFolderForWinner && MakeFolderForLoser)
            {
                // sort on playername
                PlayerNames.AddRange(Sorter.ExtractPlayers(PlayerType.All, WinnersAndLosers));
            }
            else if (MakeFolderForWinner)
            {
                PlayerNames.AddRange(Sorter.ExtractPlayers(PlayerType.Winner, Winners));
            }
            else if (MakeFolderForLoser)
            {
                PlayerNames.AddRange(Sorter.ExtractPlayers(PlayerType.Loser, Losers));
            }

            // create sort directory, and directories for each player, depending on arguments
            string sortDirectory = CurrentDirectory + @"\" + SortCriteria.ToString();
            Sorter.CreateDirectory(sortDirectory);

            foreach (var player in PlayerNames/*.Distinct()*/)
            {
                try
                {
                    var PlayerName = player;
                    PlayerName = Sorter.RemoveInvalidChars(PlayerName);
                    Directory.CreateDirectory(sortDirectory + @"\" + PlayerName);
                    IDictionary<string, IReplay> FileReplays = new Dictionary<string, IReplay>();
                    DirectoryFileReplay.Add(new KeyValuePair<string, IDictionary<string, IReplay>>(sortDirectory + @"\" + PlayerName, FileReplays));
                }
                // check each player name for invalid characters, or just catch exception and then fix it? the latter seems much more efficient, but may be bad practice? 
                catch (Exception)
                {
                    Console.WriteLine("Could not create folder for {0}", player);
                }
                
            }

            // now add all replays associated with player into the folder

            foreach (var replay in Sorter.ListReplays)
            {
                // get players per replay
                var ParsePlayers = replay.Players.ToList();
                var index = Sorter.ListReplays.IndexOf(replay);
                var FilePath = Sorter.Files.ElementAt(index);
                var DirectoryName = Directory.GetParent(FilePath);
                var FileName = FilePath.Substring(DirectoryName.ToString().Length);

                if (MakeFolderForWinner && MakeFolderForLoser)
                {
                    try
                    {
                        foreach (var aPlayer in ParsePlayers)
                        {
                            // for each player, get proper folder
                            // find the corresponding replay file
                            // add this file to that folder
                            var PlayerName = aPlayer.Name;
                            PlayerName = Sorter.RemoveInvalidChars(PlayerName);
                            var DestinationFilePath = sortDirectory + @"\" + PlayerName + FileName;
                            if (!KeepOriginalReplayNames)
                            {
                                DestinationFilePath = sortDirectory + @"\" + PlayerName + @"\" + ReplayHandler.GenerateReplayName(replay, Sorter.CustomReplayFormat) + ".rep";
                            }
                            try
                            {
                                while (File.Exists(DestinationFilePath))
                                {
                                    DestinationFilePath = Sorter.AdjustName(DestinationFilePath, false);
                                }
                                if (IsNested == true && aPlayer == ParsePlayers.Last()) 
                                {
                                    File.Move(FilePath, DestinationFilePath);
                                }
                                else
                                {
                                    File.Copy(FilePath, DestinationFilePath);
                                }
                                DirectoryFileReplay[sortDirectory + @"\" + PlayerName].Add(new KeyValuePair<string, IReplay>(DestinationFilePath, replay));
                            }
                            catch (IOException IOex)
                            {
                                Console.WriteLine(IOex.Message);
                            }
                            catch (NotSupportedException NSE)
                            {
                                Console.WriteLine(NSE.Message);
                            }
                        }
                    }
                    catch (Exception)
                    {
                        Console.WriteLine("Problem with replay: {0}", FilePath);
                    }
                }
                else if (MakeFolderForWinner)
                {
                    try
                    {
                        foreach (var player in replay.Winner)
                        {
                            var PlayerName = player.Name;
                            PlayerName = Sorter.RemoveInvalidChars(PlayerName);
                            var DestinationFilePath = sortDirectory + @"\" + PlayerName + FileName;
                            if (!KeepOriginalReplayNames)
                            {
                                DestinationFilePath = sortDirectory + @"\" + PlayerName + @"\" + ReplayHandler.GenerateReplayName(replay, Sorter.CustomReplayFormat) + ".rep";
                            }
                            try
                            {
                                while (File.Exists(DestinationFilePath))
                                {
                                    DestinationFilePath = Sorter.AdjustName(DestinationFilePath, false);
                                }
                                if (IsNested == true && player == ParsePlayers.Last())
                                {
                                    File.Move(FilePath, DestinationFilePath);
                                }
                                else
                                {
                                    File.Copy(FilePath, DestinationFilePath);
                                }
                                DirectoryFileReplay[sortDirectory + @"\" + PlayerName].Add(new KeyValuePair<string, IReplay>(DestinationFilePath, replay));
                            }
                            catch (IOException IOex)
                            {
                                Console.WriteLine(IOex.Message);
                            }
                            catch (NotSupportedException NSE)
                            {
                                Console.WriteLine(NSE.Message);
                            }
                        }

                    }
                    catch (Exception)
                    {
                        Console.WriteLine("Cannot create folder since replay has no winner.");
                    }
                }
                else if (MakeFolderForLoser)
                {
                    try
                    {
                        if (replay.Winner.Count() != 0)
                        {
                            foreach (var aPlayer in ParsePlayers)
                            {
                                if (!replay.Winner.Contains(aPlayer))
                                {
                                    var PlayerName = aPlayer.Name;
                                    PlayerName = Sorter.RemoveInvalidChars(PlayerName);
                                    var DestinationFilePath = sortDirectory + @"\" + PlayerName + FileName;
                                    if (!KeepOriginalReplayNames)
                                    {
                                        DestinationFilePath = sortDirectory + @"\" + PlayerName + @"\" + ReplayHandler.GenerateReplayName(replay, Sorter.CustomReplayFormat) + ".rep";
                                    }
                                    try
                                    {
                                        while (File.Exists(DestinationFilePath))
                                        {
                                            DestinationFilePath = Sorter.AdjustName(DestinationFilePath, false);
                                        }
                                        if (IsNested == true && aPlayer == ParsePlayers.Last())
                                        {
                                            File.Move(FilePath, DestinationFilePath);
                                        }
                                        else
                                        {
                                            File.Copy(FilePath, DestinationFilePath);
                                        }
                                        DirectoryFileReplay[sortDirectory + @"\" + PlayerName].Add(new KeyValuePair<string, IReplay>(DestinationFilePath, replay));
                                    }
                                    catch (IOException IOex)
                                    {
                                        Console.WriteLine(IOex.Message);
                                    }
                                    catch (NotSupportedException NSE)
                                    {
                                        Console.WriteLine(NSE.Message);
                                    }
                                }
                            }
                        }
                        else
                        {
                            Console.WriteLine("No winner.");
                            foreach (var aPlayer in ParsePlayers)
                            {
                                var PlayerName = aPlayer.Name;
                                PlayerName = Sorter.RemoveInvalidChars(PlayerName);
                                var DestinationFilePath = sortDirectory + @"\" + PlayerName + FileName;
                                if (!KeepOriginalReplayNames)
                                {
                                    DestinationFilePath = sortDirectory + @"\" + PlayerName + @"\" + ReplayHandler.GenerateReplayName(replay, Sorter.CustomReplayFormat) + ".rep";
                                }
                                try
                                {
                                    while (File.Exists(DestinationFilePath))
                                    {
                                        DestinationFilePath = Sorter.AdjustName(DestinationFilePath, false);
                                    }
                                    if (IsNested == true && aPlayer == ParsePlayers.Last())
                                    {
                                        File.Move(FilePath, DestinationFilePath);
                                    }
                                    else
                                    {
                                        File.Copy(FilePath, DestinationFilePath);
                                    }
                                    DirectoryFileReplay[sortDirectory + @"\" + PlayerName].Add(new KeyValuePair<string, IReplay>(DestinationFilePath, replay));
                                }
                                catch (IOException IOex)
                                {
                                    Console.WriteLine(IOex.Message);
                                }
                                catch (NotSupportedException NSE)
                                {
                                    Console.WriteLine(NSE.Message);
                                }
                            }
                        }
                    }
                    catch (Exception)
                    {
                        Console.WriteLine("Problem with replay: {0}", FilePath);
                    }
                }
                else
                {
                    try
                    {
                        var DestinationFilePath = sortDirectory + FileName;
                        if (!KeepOriginalReplayNames)
                        {
                            DestinationFilePath = sortDirectory + @"\" + ReplayHandler.GenerateReplayName(replay, Sorter.CustomReplayFormat) + ".rep";
                        }
                        try
                        {
                            while (File.Exists(DestinationFilePath))
                            {
                                DestinationFilePath = Sorter.AdjustName(DestinationFilePath, false);
                            }
                            if (IsNested == false)
                            {
                                File.Copy(FilePath, DestinationFilePath);
                            }
                            else
                            {
                                File.Move(FilePath, DestinationFilePath);
                            }
                            DirectoryFileReplay[sortDirectory].Add(new KeyValuePair<string, IReplay>(DestinationFilePath, replay));
                        }
                        catch (IOException IOex)
                        {
                            Console.WriteLine(IOex.Message);
                        }
                        catch (NotSupportedException NSE)
                        {
                            Console.WriteLine(NSE.Message);
                        }
                    }
                    catch (Exception)
                    {
                        Console.WriteLine("Problem with replay: {0}", FilePath);
                    }

                }
            }
            // not implemented yet
            return DirectoryFileReplay;
        }
    }
}
