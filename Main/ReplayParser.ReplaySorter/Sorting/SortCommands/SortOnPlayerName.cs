using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ReplayParser.ReplaySorter.Sorting;
using System.IO;
using ReplayParser.Interfaces;
using ReplayParser.ReplaySorter.Diagnostics;
using System.ComponentModel;

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
                PlayerNames.AddRange(ExtractPlayers(PlayerType.All, WinnersAndLosers));
            }
            else if (MakeFolderForWinner)
            {
                PlayerNames.AddRange(ExtractPlayers(PlayerType.Winner, Winners));
            }
            else if (MakeFolderForLoser)
            {
                PlayerNames.AddRange(ExtractPlayers(PlayerType.Loser, Losers));
            }

            // create sort directory, and directories for each player, depending on arguments
            string sortDirectory = CurrentDirectory + @"\" + SortCriteria.ToString();
            sortDirectory = Sorter.CreateDirectory(sortDirectory);

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
                                ErrorLogger.LogError($"SortOnPlayerName IOException", Sorter.OriginalDirectory + @"\\LogErrors", IOex);
                                //Console.WriteLine(IOex.Message);
                            }
                            catch (NotSupportedException NSE)
                            {
                                ErrorLogger.LogError($"SortOnPlayerName NotSupportedException", Sorter.OriginalDirectory + @"LogErrors", NSE);
                                //Console.WriteLine(NSE.Message);
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
                                ErrorLogger.LogError($"SortOnPlayerName IOException", Sorter.OriginalDirectory + @"\LogErrors", IOex);
                                //Console.WriteLine(IOex.Message);
                            }
                            catch (NotSupportedException NSE)
                            {
                                ErrorLogger.LogError($"SortOnPlayerName NotSupportedException", Sorter.OriginalDirectory + @"LogErrors", NSE);
                                //Console.WriteLine(NSE.Message);
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
                                        ErrorLogger.LogError($"SortOnPlayerName IOException", Sorter.OriginalDirectory + @"\LogErrors", IOex);
                                        //Console.WriteLine(IOex.Message);
                                    }
                                    catch (NotSupportedException NSE)
                                    {
                                        ErrorLogger.LogError($"SortOnPlayerName NotSupportedException", Sorter.OriginalDirectory + @"LogErrors", NSE);
                                        //Console.WriteLine(NSE.Message);
                                    }
                                }
                            }
                        }
                        else
                        {
                            //Console.WriteLine("No winner.");
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
                                    ErrorLogger.LogError($"SortOnPlayerName IOException", Sorter.OriginalDirectory + @"\LogErrors", IOex);
                                    //Console.WriteLine(IOex.Message);
                                }
                                catch (NotSupportedException NSE)
                                {
                                    ErrorLogger.LogError($"SortOnPlayerName NotSupportedException", Sorter.OriginalDirectory + @"\LogErrors", NSE);
                                    //Console.WriteLine(NSE.Message);
                                }
                            }
                        }
                    }
                    catch (Exception)
                    {
                        //Console.WriteLine("Problem with replay: {0}", FilePath);
                        Console.WriteLine("Replay has no winner.");
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
                            ErrorLogger.LogError($"SortOnPlayerName IOException", Sorter.OriginalDirectory + @"\LogErrors", IOex);
                            //Console.WriteLine(IOex.Message);
                        }
                        catch (NotSupportedException NSE)
                        {
                            ErrorLogger.LogError($"SortOnPlayerName NotSupportedException", Sorter.OriginalDirectory + @"\LogErrors", NSE);
                            //Console.WriteLine(NSE.Message);
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

        private List<string> ExtractPlayers(PlayerType playertype, List<string> players)
        {
            if (playertype.HasFlag(PlayerType.Winner))
            {
                foreach (var replay in Sorter.ListReplays)
                {
                    var parseplayers = replay.Players.ToList();
                    foreach (var aplayer in parseplayers)
                    {
                        // checking a list for each replay is slow... maybe define a Dictionary instead??
                        // I really think I need a player class with match history, wins/losses/...
                        if (!players.Contains(aplayer.Name))
                        {
                            try
                            {
                                if (replay.Winner.Contains(aplayer))
                                {
                                    players.Add(aplayer.Name);
                                }
                            }
                            catch (Exception ex)
                            {
                                ErrorLogger.LogError($"ExtractPlayers Winner", Sorter.OriginalDirectory + @"\LogErrors", ex);
                                //Console.WriteLine("No winner.");
                                //Console.WriteLine(ex.Message);
                            }
                        }
                    }
                }
            }
            if (playertype.HasFlag(PlayerType.Loser))
            {
                foreach (var replay in Sorter.ListReplays)
                {
                    var parseplayers = replay.Players.ToList();
                    foreach (var aplayer in parseplayers)
                    {
                        // checking a list for each replay is slow... maybe define a Dictionary instead??
                        // I really think I need a player class with match history, wins/losses/...
                        if (!players.Contains(aplayer.Name))
                        {
                            try
                            {
                                if (!replay.Winner.Contains(aplayer))
                                {
                                    players.Add(aplayer.Name);
                                }
                            }
                            catch (Exception ex)
                            {
                                players.Add(aplayer.Name);
                                ErrorLogger.LogError($"ExtractPlayers Loser", Sorter.OriginalDirectory + @"\LogErrors", ex);
                                //Console.WriteLine("No winner.");
                                //Console.WriteLine(ex.Message);
                            }
                        }
                    }
                }
            }
            if (playertype.HasFlag(PlayerType.Player))
            {
                // without observers..
            }
            if (playertype.HasFlag(PlayerType.All))
            {
                foreach (var replay in Sorter.ListReplays)
                {
                    var parseplayers = replay.Players.ToList();
                    foreach (var aplayer in parseplayers)
                    {
                        // checking a list for each replay is slow... maybe define a Dictionary instead??
                        // I really think I need a player class with match history, wins/losses/...
                        if (!players.Contains(aplayer.Name))
                        {
                            players.Add(aplayer.Name);
                        }
                    }
                }
            }
            return players;
        }

        public IDictionary<string, IDictionary<string, IReplay>> SortAsync(BackgroundWorker worker_ReplaySorter, int currentCriteria, int numberOfCriteria, int currentPositionNested, int numberOfPositions)
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
                PlayerNames.AddRange(ExtractPlayers(PlayerType.All, WinnersAndLosers));
            }
            else if (MakeFolderForWinner)
            {
                PlayerNames.AddRange(ExtractPlayers(PlayerType.Winner, Winners));
            }
            else if (MakeFolderForLoser)
            {
                PlayerNames.AddRange(ExtractPlayers(PlayerType.Loser, Losers));
            }

            // create sort directory, and directories for each player, depending on arguments
            string sortDirectory = CurrentDirectory + @"\" + SortCriteria.ToString();
            sortDirectory = Sorter.CreateDirectory(sortDirectory, true);

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
            int currentPosition = 0;
            int progressPercentage = 0;

            foreach (var replay in Sorter.ListReplays)
            {
                if (worker_ReplaySorter.CancellationPending == true)
                {
                    return null;
                }
                currentPosition++;
                if (this.IsNested == false)
                {
                    progressPercentage = Convert.ToInt32(((double)currentPosition / Sorter.ListReplays.Count) * 1 / numberOfCriteria * 100);
                }
                else
                {
                    progressPercentage = Convert.ToInt32((((double)currentPosition / Sorter.ListReplays.Count) * 1 / numberOfPositions * 100 + ((currentPositionNested - 1) * 100 / numberOfPositions)) * ((double)1 / numberOfCriteria));
                    progressPercentage += (currentCriteria - 1) * 100 / numberOfCriteria;
                }
                worker_ReplaySorter.ReportProgress(progressPercentage, "sorting on playername...");

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
                                ErrorLogger.LogError($"SortOnPlayerName IOException", Sorter.OriginalDirectory + @"\\LogErrors", IOex);
                                //Console.WriteLine(IOex.Message);
                            }
                            catch (NotSupportedException NSE)
                            {
                                ErrorLogger.LogError($"SortOnPlayerName NotSupportedException", Sorter.OriginalDirectory + @"LogErrors", NSE);
                                //Console.WriteLine(NSE.Message);
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
                                ErrorLogger.LogError($"SortOnPlayerName IOException", Sorter.OriginalDirectory + @"\LogErrors", IOex);
                                //Console.WriteLine(IOex.Message);
                            }
                            catch (NotSupportedException NSE)
                            {
                                ErrorLogger.LogError($"SortOnPlayerName NotSupportedException", Sorter.OriginalDirectory + @"LogErrors", NSE);
                                //Console.WriteLine(NSE.Message);
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
                                        ErrorLogger.LogError($"SortOnPlayerName IOException", Sorter.OriginalDirectory + @"\LogErrors", IOex);
                                        //Console.WriteLine(IOex.Message);
                                    }
                                    catch (NotSupportedException NSE)
                                    {
                                        ErrorLogger.LogError($"SortOnPlayerName NotSupportedException", Sorter.OriginalDirectory + @"LogErrors", NSE);
                                        //Console.WriteLine(NSE.Message);
                                    }
                                }
                            }
                        }
                        else
                        {
                            //Console.WriteLine("No winner.");
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
                                    ErrorLogger.LogError($"SortOnPlayerName IOException", Sorter.OriginalDirectory + @"\LogErrors", IOex);
                                    //Console.WriteLine(IOex.Message);
                                }
                                catch (NotSupportedException NSE)
                                {
                                    ErrorLogger.LogError($"SortOnPlayerName NotSupportedException", Sorter.OriginalDirectory + @"\LogErrors", NSE);
                                    //Console.WriteLine(NSE.Message);
                                }
                            }
                        }
                    }
                    catch (Exception)
                    {
                        //Console.WriteLine("Problem with replay: {0}", FilePath);
                        Console.WriteLine("Replay has no winner.");
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
                            ErrorLogger.LogError($"SortOnPlayerName IOException", Sorter.OriginalDirectory + @"\LogErrors", IOex);
                            //Console.WriteLine(IOex.Message);
                        }
                        catch (NotSupportedException NSE)
                        {
                            ErrorLogger.LogError($"SortOnPlayerName NotSupportedException", Sorter.OriginalDirectory + @"\LogErrors", NSE);
                            //Console.WriteLine(NSE.Message);
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
