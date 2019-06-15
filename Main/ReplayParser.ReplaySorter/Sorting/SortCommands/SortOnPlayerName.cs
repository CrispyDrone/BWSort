using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using ReplayParser.Interfaces;
using ReplayParser.ReplaySorter.Diagnostics;
using System.ComponentModel;
using ReplayParser.ReplaySorter.IO;
using ReplayParser.ReplaySorter.Renaming;

namespace ReplayParser.ReplaySorter.Sorting.SortCommands
{
    public class SortOnPlayerName : ISortCommand
    {
        #region private

        #region methods

        private IEnumerable<string> ExtractPlayers(IEnumerable<IReplay> replays, PlayerType playerType)
        {
            var playerList = new HashSet<string>();

            foreach (var replay in replays)
            {
                var players = GetPlayers(playerType, replay);

                foreach (var player in replay.Players)
                {
                    if (playerList.Contains(player.Name))
                        continue;

                    playerList.Add(player.Name);
                }
            }

            return playerList.AsEnumerable();
        }

        private IEnumerable<IPlayer> GetPlayers(PlayerType type, IReplay replay)
        {
            switch ((int)type)
            {
                // none
                case 0:
                    return Enumerable.Empty<IPlayer>();
                    
                // winners
                case 1:
                    return replay.Winners;

                // losers
                case 2:
                    return replay.Players.Except(replay.Observers).Except(replay.Winners);

                // winners + losers
                case 3:
                    return replay.Players.Except(replay.Observers);

                // observers
                case 4:
                    return replay.Observers;

                // winners + observers
                case 5:
                    return replay.Observers.Union(replay.Winners);

                // losers + observers
                case 6:
                    return replay.Players.Except(replay.Winners);

                // winners + losers + observers
                case 7:
                    return replay.Players;

                default:
                    throw new Exception();
            }
        }

        private PlayerType GetPlayerType(bool makeFolderForWinner, bool makeFolderForLoser)
        {
            if (makeFolderForWinner && makeFolderForLoser)
                return PlayerType.Winner | PlayerType.Loser;

            if (makeFolderForWinner)
                return PlayerType.Winner;

            if (makeFolderForLoser)
                return PlayerType.Loser;

            return PlayerType.None;
        }

        private bool MoveOrCopyReplayToPlayerFolders(File<IReplay> replay, IEnumerable<IPlayer> players, IDictionary<string, List<File<IReplay>>> directoryFileReplay)
        {
            bool threwException = false;

            foreach (var player in players)
            {
                if (!MoveAndRenameReplay(
                        replay,
                        Sorter.CurrentDirectory + @"\" + SortCriteria.ToString(),
                        player.Name,
                        !(IsNested == true && player == players.Last()),
                        KeepOriginalReplayNames,
                        Sorter.CustomReplayFormat,
                        directoryFileReplay))
                {
                    threwException = true;
                }
            }

            return threwException;
        }

        private bool MoveAndRenameReplay(File<IReplay> replay, string sortDirectory, string FolderName, bool shouldCopy, bool KeepOriginalReplayNames, CustomReplayFormat CustomReplayFormat, IDictionary<string, List<File<IReplay>>> directoryFileReplay)
        {
            bool threwException = false;
            FolderName = FileHandler.RemoveInvalidChars(FolderName);

            try
            {
                if (shouldCopy)
                {
                    ReplayHandler.CopyReplay(replay, sortDirectory, FolderName, KeepOriginalReplayNames, Sorter.CustomReplayFormat);
                    var additionalReplayCreated = File<IReplay>.Create(replay.Content, replay.FilePath, replay.Hash);
                    replay.Rewind();
                    directoryFileReplay[sortDirectory + @"\" + FolderName].Add(additionalReplayCreated);
                }
                else
                {
                    ReplayHandler.MoveReplay(replay, sortDirectory, FolderName, KeepOriginalReplayNames, Sorter.CustomReplayFormat);
                }
                directoryFileReplay[sortDirectory + @"\" + FolderName].Add(replay);
            }
            catch (Exception ex)
            {
                threwException = true;
                ErrorLogger.GetInstance()?.LogError($"{DateTime.Now} - SortOnPlayerName exception: {replay.OriginalFilePath}", ex: ex);
            }
            return !threwException;
        }


        #endregion

        #endregion

        #region public

        #region constructor

        public SortOnPlayerName(SortCriteriaParameters sortcriteriaparameters, bool keeporiginalreplaynames, Sorter sorter)
        {
            SortCriteriaParameters = sortcriteriaparameters;
            KeepOriginalReplayNames = keeporiginalreplaynames;
            Sorter = sorter;
        }

        #endregion

        #region properties

        public bool KeepOriginalReplayNames { get; set; }
        public SortCriteriaParameters SortCriteriaParameters { get; set; }
        public Sorter Sorter { get; set; }
        public Criteria SortCriteria { get { return Criteria.PLAYERNAME; } }
        public bool IsNested { get; set; }

        #endregion

        #region methods

        public IDictionary<string, List<File<IReplay>>> Sort(List<string> replaysThrowingExceptions)
        {
            // Dictionary<directory, dictionary<file, replay>>
            IDictionary<string, List<File<IReplay>>> directoryFileReplay = new Dictionary<string, List<File<IReplay>>>();

            List<string> playerNames = new List<string>();
            bool MakeFolderForWinner = (bool)SortCriteriaParameters.MakeFolderForWinner;
            bool MakeFolderForLoser = (bool)SortCriteriaParameters.MakeFolderForLoser;
            string CurrentDirectory = Sorter.CurrentDirectory;
            Criteria SortCriteria = Sorter.SortCriteria;

            playerNames.AddRange(ExtractPlayers(Sorter.ListReplays.Select(f => f.Content).AsEnumerable(), GetPlayerType(MakeFolderForWinner, MakeFolderForLoser)));

            // create sort directory, and directories for each player, depending on arguments
            string sortDirectory = Sorter.CurrentDirectory;
            if (!(IsNested && !Sorter.GenerateIntermediateFolders))
            {
                if (IsNested)
                {
                    sortDirectory = Sorter.CurrentDirectory + @"\" + SortCriteria;
                }
                else
                {
                    sortDirectory = Sorter.CurrentDirectory + @"\" + string.Join(",", Sorter.CriteriaStringOrder);
                }
                sortDirectory = FileHandler.CreateDirectory(sortDirectory);
            }

            foreach (var player in playerNames/*.Distinct()*/)
            {
                try
                {
                    var PlayerName = player;
                    PlayerName = FileHandler.RemoveInvalidChars(PlayerName);
                    Directory.CreateDirectory(sortDirectory + @"\" + PlayerName);
                    var FileReplays = new List<File<IReplay>>();
                    directoryFileReplay.Add(new KeyValuePair<string, List<File<IReplay>>>(sortDirectory + @"\" + PlayerName, FileReplays));
                }
                // check each player name for invalid characters, or just catch exception and then fix it? the latter seems much more efficient, but may be bad practice? WRONG throwing and catching exceptions is very expensive
                catch (Exception ex)
                {
                    ErrorLogger.GetInstance()?.LogError($"{DateTime.Now} - Could not create folder for {player}", ex: ex);
                }
                
            }

            // now add all replays associated with player into the folder

            foreach (var replay in Sorter.ListReplays)
            {
                bool threwException = false;
                // get players per replay
                var ParsePlayers = replay.Content.Players.ToList();
                // var index = Sorter.ListReplays.IndexOf(replay);
                var sourceFilePath = replay.FilePath;
                var DirectoryName = Directory.GetParent(sourceFilePath);
                var FileName = sourceFilePath.Substring(DirectoryName.ToString().Length);

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
                            if (IsNested == true && aPlayer == ParsePlayers.Last())
                            {
                                threwException = !MoveAndRenameReplay(replay, sortDirectory, PlayerName, false, KeepOriginalReplayNames, Sorter.CustomReplayFormat, directoryFileReplay);
                            }
                            else
                            {
                                threwException = !MoveAndRenameReplay(replay, sortDirectory, PlayerName, true, KeepOriginalReplayNames, Sorter.CustomReplayFormat, directoryFileReplay);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        threwException = true;
                        ErrorLogger.GetInstance()?.LogError($"{DateTime.Now} - Problem with replay: {replay.FilePath}", ex: ex);
                    }
                }
                else if (MakeFolderForWinner)
                {
                    try
                    {
                        foreach (var player in replay.Content.Winners)
                        {
                            var PlayerName = player.Name;

                            if (IsNested == true && player == ParsePlayers.Last())
                            {
                                threwException = !MoveAndRenameReplay(replay, sortDirectory, PlayerName, false, KeepOriginalReplayNames, Sorter.CustomReplayFormat, directoryFileReplay);
                            }
                            else
                            {
                                threwException = !MoveAndRenameReplay(replay, sortDirectory, PlayerName, true, KeepOriginalReplayNames, Sorter.CustomReplayFormat, directoryFileReplay);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        threwException = true;
                        ErrorLogger.GetInstance()?.LogError($"{DateTime.Now} - Cannot create folder since replay has no winner.", ex: ex);
                    }
                }
                else if (MakeFolderForLoser)
                {
                    try
                    {
                        if (replay.Content.Winners.Count() != 0)
                        {
                            foreach (var aPlayer in ParsePlayers)
                            {
                                if (!replay.Content.Winners.Contains(aPlayer))
                                {
                                    var PlayerName = aPlayer.Name;

                                    if (IsNested == true && aPlayer == ParsePlayers.Last())
                                    {
                                        threwException = !MoveAndRenameReplay(replay, sortDirectory, PlayerName, false, KeepOriginalReplayNames, Sorter.CustomReplayFormat, directoryFileReplay);
                                    }
                                    else
                                    {
                                        threwException = !MoveAndRenameReplay(replay, sortDirectory, PlayerName, true, KeepOriginalReplayNames, Sorter.CustomReplayFormat, directoryFileReplay);
                                    }
                                }
                            }
                        }
                        else
                        {
                            foreach (var aPlayer in ParsePlayers)
                            {
                                var PlayerName = aPlayer.Name;

                                if (IsNested == true && aPlayer == ParsePlayers.Last())
                                {
                                    threwException = !MoveAndRenameReplay(replay, sortDirectory, PlayerName, false, KeepOriginalReplayNames, Sorter.CustomReplayFormat, directoryFileReplay);
                                }
                                else
                                {
                                    threwException = !MoveAndRenameReplay(replay, sortDirectory, PlayerName, true, KeepOriginalReplayNames, Sorter.CustomReplayFormat, directoryFileReplay);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        threwException = true;
                        ErrorLogger.GetInstance()?.LogError("Replay has no winner", ex: ex);
                    }
                }
                else
                {
                    try
                    {
                        if (IsNested == false)
                        {
                            threwException = !MoveAndRenameReplay(replay, sortDirectory, string.Empty, true, KeepOriginalReplayNames, Sorter.CustomReplayFormat, directoryFileReplay);
                        }
                        else
                        {
                            threwException = !MoveAndRenameReplay(replay, sortDirectory, string.Empty, false, KeepOriginalReplayNames, Sorter.CustomReplayFormat, directoryFileReplay);
                        }
                    }
                    catch (Exception ex)
                    {
                        threwException = true;
                        ErrorLogger.GetInstance()?.LogError($"{DateTime.Now} - Problem with replay: {replay.FilePath}", ex: ex);
                    }

                }
                if (threwException)
                    replaysThrowingExceptions.Add(replay.OriginalFilePath);
            }
            return directoryFileReplay;
        }

        public IDictionary<string, List<File<IReplay>>> SortAsync(List<string> replaysThrowingExceptions, BackgroundWorker worker_ReplaySorter, int currentCriteria, int numberOfCriteria, int currentPositionNested, int numberOfPositions)
        {
            // Dictionary<directory, dictionary<file, replay>>
            IDictionary<string, List<File<IReplay>>> directoryFileReplay = new Dictionary<string, List<File<IReplay>>>();

            List<string> playerNames = new List<string>();
            bool makeFolderForWinner = (bool)SortCriteriaParameters.MakeFolderForWinner;
            bool makeFolderForLoser = (bool)SortCriteriaParameters.MakeFolderForLoser;
            string CurrentDirectory = Sorter.CurrentDirectory;
            Criteria SortCriteria = Sorter.SortCriteria;

            playerNames.AddRange(ExtractPlayers(Sorter.ListReplays.Select(f => f.Content).AsEnumerable(), GetPlayerType(makeFolderForWinner, makeFolderForLoser)));

            // create sort directory, and directories for each player, depending on arguments
            string sortDirectory = Sorter.CurrentDirectory;
            if (!(IsNested && !Sorter.GenerateIntermediateFolders))
            {
                if (IsNested)
                {
                    sortDirectory = Sorter.CurrentDirectory + @"\" + SortCriteria;
                }
                else
                {
                    sortDirectory = Sorter.CurrentDirectory + @"\" + string.Join(",", Sorter.CriteriaStringOrder);
                }
                sortDirectory = FileHandler.CreateDirectory(sortDirectory, true);
            }

            foreach (var player in playerNames/*.Distinct()*/)
            {
                try
                {
                    var PlayerName = player;
                    PlayerName = FileHandler.RemoveInvalidChars(PlayerName);
                    Directory.CreateDirectory(sortDirectory + @"\" + PlayerName);
                    var FileReplays = new List<File<IReplay>>();
                    directoryFileReplay.Add(new KeyValuePair<string, List<File<IReplay>>>(sortDirectory + @"\" + PlayerName, FileReplays));
                }
                catch (Exception ex)
                {
                    ErrorLogger.GetInstance()?.LogError($"{DateTime.Now} - Could not create folder for {player}", ex: ex);
                }
            }

            // now add all replays associated with player into the folder
            int currentPosition = 0;
            int progressPercentage = 0;

            foreach (var replay in Sorter.ListReplays)
            {
                bool threwException = false;
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
                var ParsePlayers = replay.Content.Players.ToList();
                var sourceFilePath = replay.FilePath;
                var DirectoryName = Directory.GetParent(sourceFilePath);
                var FileName = sourceFilePath.Substring(DirectoryName.ToString().Length);

                if (makeFolderForWinner && makeFolderForLoser)
                {
                    try
                    {
                        foreach (var aPlayer in ParsePlayers)
                        {
                            // for each player, get proper folder
                            // find the corresponding replay file
                            // add this file to that folder
                            var PlayerName = aPlayer.Name;
                            if (IsNested == true && aPlayer == ParsePlayers.Last())
                            {
                                threwException = !MoveAndRenameReplay(replay, sortDirectory, PlayerName, false, KeepOriginalReplayNames, Sorter.CustomReplayFormat, directoryFileReplay);
                            }
                            else
                            {
                                threwException = !MoveAndRenameReplay(replay, sortDirectory, PlayerName, true, KeepOriginalReplayNames, Sorter.CustomReplayFormat, directoryFileReplay);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        threwException = true;
                        ErrorLogger.GetInstance()?.LogError($"{DateTime.Now} - Problem with replay {sourceFilePath}", ex: ex);
                    }
                }
                else if (makeFolderForWinner)
                {
                    try
                    {
                        foreach (var player in replay.Content.Winners)
                        {
                            var PlayerName = player.Name;
                            if (IsNested == true && player == ParsePlayers.Last())
                            {
                                threwException = !MoveAndRenameReplay(replay, sortDirectory, PlayerName, false, KeepOriginalReplayNames, Sorter.CustomReplayFormat, directoryFileReplay);
                            }
                            else
                            {
                                threwException = !MoveAndRenameReplay(replay, sortDirectory, PlayerName, true, KeepOriginalReplayNames, Sorter.CustomReplayFormat, directoryFileReplay);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        threwException = true;
                        ErrorLogger.GetInstance()?.LogError("Cannot create folder since replay has no winner.", ex: ex);
                    }
                }
                else if (makeFolderForLoser)
                {
                    try
                    {
                        if (replay.Content.Winners.Count() != 0)
                        {
                            foreach (var aPlayer in ParsePlayers)
                            {
                                if (!replay.Content.Winners.Contains(aPlayer))
                                {
                                    var PlayerName = aPlayer.Name;
                                    if (IsNested == true && aPlayer == ParsePlayers.Last())
                                    {
                                        threwException = !MoveAndRenameReplay(replay, sortDirectory, PlayerName, false, KeepOriginalReplayNames, Sorter.CustomReplayFormat, directoryFileReplay);
                                    }
                                    else
                                    {
                                        threwException = !MoveAndRenameReplay(replay, sortDirectory, PlayerName, true, KeepOriginalReplayNames, Sorter.CustomReplayFormat, directoryFileReplay);
                                    }
                                }
                            }
                        }
                        else
                        {
                            foreach (var aPlayer in ParsePlayers)
                            {
                                var PlayerName = aPlayer.Name;

                                if (IsNested == true && aPlayer == ParsePlayers.Last())
                                {
                                    threwException = !MoveAndRenameReplay(replay, sortDirectory, PlayerName, false, KeepOriginalReplayNames, Sorter.CustomReplayFormat, directoryFileReplay);
                                }
                                else
                                {
                                    threwException = !MoveAndRenameReplay(replay, sortDirectory, PlayerName, true, KeepOriginalReplayNames, Sorter.CustomReplayFormat, directoryFileReplay);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        threwException = true;
                        ErrorLogger.GetInstance()?.LogError("Replay has no winner.", ex: ex);
                    }
                }
                else
                {
                    try
                    {
                        var DestinationFilePath = sortDirectory + FileName;
                        if (IsNested == false)
                        {
                            threwException = !MoveAndRenameReplay(replay, sortDirectory, string.Empty, true, KeepOriginalReplayNames, Sorter.CustomReplayFormat, directoryFileReplay);
                        }
                        else
                        {
                            threwException = !MoveAndRenameReplay(replay, sortDirectory, string.Empty, false, KeepOriginalReplayNames, Sorter.CustomReplayFormat, directoryFileReplay);
                        }
                    }
                    catch (Exception ex)
                    {
                        threwException = true;
                        ErrorLogger.GetInstance()?.LogError("Problem with replay: {0}", ex: ex);
                    }

                }
                if (threwException)
                    replaysThrowingExceptions.Add(replay.OriginalFilePath);
            }
            return directoryFileReplay;
        }

        public IDictionary<string, List<File<IReplay>>> PreviewSort(List<string> replaysThrowingExceptions, BackgroundWorker worker_ReplaySorter, int currentCriteria, int numberOfCriteria, int currentPositionNested = 0, int numberOfPositions = 0)
        {
            IDictionary<string, List<File<IReplay>>> directoryFileReplay = new Dictionary<string, List<File<IReplay>>>();

            List<string> playerNames = new List<string>();
            bool makeFolderForWinner = (bool)SortCriteriaParameters.MakeFolderForWinner;
            bool makeFolderForLoser = (bool)SortCriteriaParameters.MakeFolderForLoser;
            string CurrentDirectory = Sorter.CurrentDirectory;
            Criteria SortCriteria = Sorter.SortCriteria;

            playerNames.AddRange(ExtractPlayers(Sorter.ListReplays.Select(f => f.Content).AsEnumerable(), GetPlayerType(makeFolderForWinner, makeFolderForLoser)));

            string sortDirectory = Sorter.CurrentDirectory;
            if (!(IsNested && !Sorter.GenerateIntermediateFolders))
            {
                if (IsNested)
                {
                    sortDirectory = Sorter.CurrentDirectory + @"\" + SortCriteria;
                }
                else
                {
                    sortDirectory = Sorter.CurrentDirectory + @"\" + string.Join(",", Sorter.CriteriaStringOrder);
                }
                sortDirectory = FileHandler.AdjustName(sortDirectory, true);
            }

            foreach (var player in playerNames)
            {
                var playerName = player;
                playerName = FileHandler.RemoveInvalidChars(playerName);
                directoryFileReplay.Add(new KeyValuePair<string, List<File<IReplay>>>(sortDirectory + @"\" + playerName, new List<File<IReplay>>()));
            }

            int currentPosition = 0;
            int progressPercentage = 0;

            foreach (var replay in Sorter.ListReplays)
            {
                bool threwException = false;
                if (worker_ReplaySorter.CancellationPending == true)
                    return null;

                currentPosition++;
                if (IsNested == false)
                {
                    progressPercentage = Convert.ToInt32(((double)currentPosition / Sorter.ListReplays.Count) * 1 / numberOfCriteria * 100);
                }
                else
                {
                    progressPercentage = Convert.ToInt32((((double)currentPosition / Sorter.ListReplays.Count) * 1 / numberOfPositions * 100 + ((currentPositionNested - 1) * 100 / numberOfPositions)) * ((double)1 / numberOfCriteria));
                    progressPercentage += (currentCriteria - 1) * 100 / numberOfCriteria;
                }
                worker_ReplaySorter.ReportProgress(progressPercentage, "sorting on playername...");

                threwException = MoveOrCopyReplayToPlayerFolders(
                    replay,
                    GetPlayers(
                        GetPlayerType(makeFolderForWinner, makeFolderForLoser),
                        replay.Content
                    ),
                    directoryFileReplay
                );

                if (!(makeFolderForWinner || makeFolderForLoser))
                {
                    threwException = !MoveAndRenameReplay(replay, sortDirectory, string.Empty, !IsNested, KeepOriginalReplayNames, Sorter.CustomReplayFormat, directoryFileReplay);
                }

                if (threwException)
                    replaysThrowingExceptions.Add(replay.OriginalFilePath);
            }
            return directoryFileReplay;
        }

        #endregion

        #endregion

    }
}
