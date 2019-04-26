﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using ReplayParser.Interfaces;
using ReplayParser.ReplaySorter.Diagnostics;
using System.ComponentModel;
using ReplayParser.ReplaySorter.IO;

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
                foreach (var player in replay.Players)
                {
                    if (playerList.Contains(player.Name))
                        continue;

                    foreach (PlayerType type in Enum.GetValues(typeof(PlayerType)))
                    {
                        if ((playerType & type) != 0)
                        {
                            var playerPool = GetPlayers(type, replay);

                            if (playerPool.Contains(player))
                                playerList.Add(player.Name);

                            break;
                        }
                    }
                }
            }
            return playerList.AsEnumerable();
        }

        private IEnumerable<IPlayer> GetPlayers(PlayerType type, IReplay replay)
        {
            switch (type)
            {
                case PlayerType.Winner:
                    return replay.Winners;
                case PlayerType.Loser:
                    return replay.Players.Except(replay.Winners ?? Enumerable.Empty<IPlayer>());
                case PlayerType.All:
                    return replay.Players;
                case PlayerType.Player:
                    return replay.Players.Except(replay.Observers ?? Enumerable.Empty<IPlayer>());
                case PlayerType.None:
                    return Enumerable.Empty<IPlayer>();
                default:
                    throw new Exception();
            }
        }

        private PlayerType GetPlayerType(bool makeFolderForWinner, bool makeFolderForLoser)
        {
            if (makeFolderForWinner && makeFolderForLoser)
                return PlayerType.All;

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

        [Obsolete("Use other ExtractPlayers method")]
        private List<string> ExtractPlayers(PlayerType playertype, List<string> players)
        {
            if (playertype.HasFlag(PlayerType.Winner))
            {
                foreach (var replay in Sorter.ListReplays)
                {
                    var parseplayers = replay.Content.Players.ToList();
                    foreach (var aplayer in parseplayers)
                    {
                        // checking a list for each replay is slow... maybe define a Dictionary instead??
                        // I really think I need a player class with match history, wins/losses/...
                        if (!players.Contains(aplayer.Name))
                        {
                            try
                            {
                                if (replay.Content.Winners.Contains(aplayer))
                                {
                                    players.Add(aplayer.Name);
                                }
                            }
                            catch (Exception ex)
                            {
                                ErrorLogger.GetInstance()?.LogError($"{DateTime.Now} - ExtractPlayers Winner", ex: ex);
                            }
                        }
                    }
                }
            }
            if (playertype.HasFlag(PlayerType.Loser))
            {
                foreach (var replay in Sorter.ListReplays)
                {
                    var parseplayers = replay.Content.Players.ToList();
                    foreach (var aplayer in parseplayers)
                    {
                        // checking a list for each replay is slow... maybe define a Dictionary instead??
                        // I really think I need a player class with match history, wins/losses/...
                        if (!players.Contains(aplayer.Name))
                        {
                            try
                            {
                                if (!replay.Content.Winners.Contains(aplayer))
                                {
                                    players.Add(aplayer.Name);
                                }
                            }
                            catch (Exception ex)
                            {
                                players.Add(aplayer.Name);
                                ErrorLogger.GetInstance()?.LogError($"{DateTime.Now} - ExtractPlayers Loser", ex: ex);
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
                    var parseplayers = replay.Content.Players.ToList();
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

        private bool MoveAndRenameReplay(File<IReplay> replay, string sortDirectory, string FolderName, bool shouldCopy, bool KeepOriginalReplayNames, CustomReplayFormat CustomReplayFormat, IDictionary<string, List<File<IReplay>>> directoryFileReplay)
        {
            bool threwException = false;
            FolderName = ReplayHandler.RemoveInvalidChars(FolderName);

            try
            {
                if (shouldCopy)
                {
                    ReplayHandler.CopyReplay(replay, sortDirectory, FolderName, KeepOriginalReplayNames, Sorter.CustomReplayFormat);
                    var additionalReplayCreated = File<IReplay>.Create(replay.Content, replay.FilePath);
                    replay.Rewind();
                    directoryFileReplay[sortDirectory + @"\" + FolderName].Add(additionalReplayCreated);
                }
                else
                {
                    ReplayHandler.MoveReplay(replay, sortDirectory, FolderName, KeepOriginalReplayNames, Sorter.CustomReplayFormat);
                }
                directoryFileReplay[sortDirectory + @"\" + FolderName].Add(replay);
            }
            catch (IOException IOex)
            {
                threwException = true;
                ErrorLogger.GetInstance()?.LogError($"{DateTime.Now} - SortOnPlayerName IOException: {replay.OriginalFilePath}", ex: IOex);
            }
            catch (NotSupportedException NSE)
            {
                threwException = true;
                ErrorLogger.GetInstance()?.LogError($"{DateTime.Now} - SortOnPlayerName NotSupportedException: {replay.OriginalFilePath}", ex: NSE);
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
            IDictionary<string, List<File<IReplay>>> DirectoryFileReplay = new Dictionary<string, List<File<IReplay>>>();

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
            sortDirectory = FileHandler.CreateDirectory(sortDirectory);

            foreach (var player in PlayerNames/*.Distinct()*/)
            {
                try
                {
                    var PlayerName = player;
                    PlayerName = ReplayHandler.RemoveInvalidChars(PlayerName);
                    Directory.CreateDirectory(sortDirectory + @"\" + PlayerName);
                    var FileReplays = new List<File<IReplay>>();
                    DirectoryFileReplay.Add(new KeyValuePair<string, List<File<IReplay>>>(sortDirectory + @"\" + PlayerName, FileReplays));
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
                                threwException = !MoveAndRenameReplay(replay, sortDirectory, PlayerName, false, KeepOriginalReplayNames, Sorter.CustomReplayFormat, DirectoryFileReplay);
                            }
                            else
                            {
                                threwException = !MoveAndRenameReplay(replay, sortDirectory, PlayerName, true, KeepOriginalReplayNames, Sorter.CustomReplayFormat, DirectoryFileReplay);
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
                                threwException = !MoveAndRenameReplay(replay, sortDirectory, PlayerName, false, KeepOriginalReplayNames, Sorter.CustomReplayFormat, DirectoryFileReplay);
                            }
                            else
                            {
                                threwException = !MoveAndRenameReplay(replay, sortDirectory, PlayerName, true, KeepOriginalReplayNames, Sorter.CustomReplayFormat, DirectoryFileReplay);
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
                                        threwException = !MoveAndRenameReplay(replay, sortDirectory, PlayerName, false, KeepOriginalReplayNames, Sorter.CustomReplayFormat, DirectoryFileReplay);
                                    }
                                    else
                                    {
                                        threwException = !MoveAndRenameReplay(replay, sortDirectory, PlayerName, true, KeepOriginalReplayNames, Sorter.CustomReplayFormat, DirectoryFileReplay);
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
                                    threwException = !MoveAndRenameReplay(replay, sortDirectory, PlayerName, false, KeepOriginalReplayNames, Sorter.CustomReplayFormat, DirectoryFileReplay);
                                }
                                else
                                {
                                    threwException = !MoveAndRenameReplay(replay, sortDirectory, PlayerName, true, KeepOriginalReplayNames, Sorter.CustomReplayFormat, DirectoryFileReplay);
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
                            threwException = !MoveAndRenameReplay(replay, sortDirectory, string.Empty, true, KeepOriginalReplayNames, Sorter.CustomReplayFormat, DirectoryFileReplay);
                        }
                        else
                        {
                            threwException = !MoveAndRenameReplay(replay, sortDirectory, string.Empty, false, KeepOriginalReplayNames, Sorter.CustomReplayFormat, DirectoryFileReplay);
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
            // not implemented yet
            return DirectoryFileReplay;
        }

        public IDictionary<string, List<File<IReplay>>> SortAsync(List<string> replaysThrowingExceptions, BackgroundWorker worker_ReplaySorter, int currentCriteria, int numberOfCriteria, int currentPositionNested, int numberOfPositions)
        {
            // Dictionary<directory, dictionary<file, replay>>
            IDictionary<string, List<File<IReplay>>> DirectoryFileReplay = new Dictionary<string, List<File<IReplay>>>();

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
            sortDirectory = FileHandler.CreateDirectory(sortDirectory, true);

            foreach (var player in PlayerNames/*.Distinct()*/)
            {
                try
                {
                    var PlayerName = player;
                    PlayerName = ReplayHandler.RemoveInvalidChars(PlayerName);
                    Directory.CreateDirectory(sortDirectory + @"\" + PlayerName);
                    var FileReplays = new List<File<IReplay>>();
                    DirectoryFileReplay.Add(new KeyValuePair<string, List<File<IReplay>>>(sortDirectory + @"\" + PlayerName, FileReplays));
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
                                threwException = !MoveAndRenameReplay(replay, sortDirectory, PlayerName, false, KeepOriginalReplayNames, Sorter.CustomReplayFormat, DirectoryFileReplay);
                            }
                            else
                            {
                                threwException = !MoveAndRenameReplay(replay, sortDirectory, PlayerName, true, KeepOriginalReplayNames, Sorter.CustomReplayFormat, DirectoryFileReplay);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        threwException = true;
                        ErrorLogger.GetInstance()?.LogError($"{DateTime.Now} - Problem with replay {sourceFilePath}", ex: ex);
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
                                threwException = !MoveAndRenameReplay(replay, sortDirectory, PlayerName, false, KeepOriginalReplayNames, Sorter.CustomReplayFormat, DirectoryFileReplay);
                            }
                            else
                            {
                                threwException = !MoveAndRenameReplay(replay, sortDirectory, PlayerName, true, KeepOriginalReplayNames, Sorter.CustomReplayFormat, DirectoryFileReplay);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        threwException = true;
                        ErrorLogger.GetInstance()?.LogError("Cannot create folder since replay has no winner.", ex: ex);
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
                                        threwException = !MoveAndRenameReplay(replay, sortDirectory, PlayerName, false, KeepOriginalReplayNames, Sorter.CustomReplayFormat, DirectoryFileReplay);
                                    }
                                    else
                                    {
                                        threwException = !MoveAndRenameReplay(replay, sortDirectory, PlayerName, true, KeepOriginalReplayNames, Sorter.CustomReplayFormat, DirectoryFileReplay);
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
                                    threwException = !MoveAndRenameReplay(replay, sortDirectory, PlayerName, false, KeepOriginalReplayNames, Sorter.CustomReplayFormat, DirectoryFileReplay);
                                }
                                else
                                {
                                    threwException = !MoveAndRenameReplay(replay, sortDirectory, PlayerName, true, KeepOriginalReplayNames, Sorter.CustomReplayFormat, DirectoryFileReplay);
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
                            threwException = !MoveAndRenameReplay(replay, sortDirectory, string.Empty, true, KeepOriginalReplayNames, Sorter.CustomReplayFormat, DirectoryFileReplay);
                        }
                        else
                        {
                            threwException = !MoveAndRenameReplay(replay, sortDirectory, string.Empty, false, KeepOriginalReplayNames, Sorter.CustomReplayFormat, DirectoryFileReplay);
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
            // not implemented yet
            return DirectoryFileReplay;
        }

        public IDictionary<string, List<File<IReplay>>> PreviewSort(List<string> replaysThrowingExceptions, BackgroundWorker worker_ReplaySorter, int currentCriteria, int numberOfCriteria, int currentPositionNested = 0, int numberOfPositions = 0)
        {
            IDictionary<string, List<File<IReplay>>> DirectoryFileReplay = new Dictionary<string, List<File<IReplay>>>();

            List<string> PlayerNames = new List<string>();
            bool MakeFolderForWinner = (bool)SortCriteriaParameters.MakeFolderForWinner;
            bool MakeFolderForLoser = (bool)SortCriteriaParameters.MakeFolderForLoser;
            string CurrentDirectory = Sorter.CurrentDirectory;
            Criteria SortCriteria = Sorter.SortCriteria;

            PlayerNames.AddRange(ExtractPlayers(Sorter.ListReplays.Select(f => f.Content).AsEnumerable(), GetPlayerType(MakeFolderForWinner, MakeFolderForLoser)));

            string sortDirectory = CurrentDirectory + @"\" + SortCriteria.ToString();
            sortDirectory = FileHandler.AdjustName(sortDirectory, true);

            foreach (var player in PlayerNames)
            {
                var PlayerName = player;
                PlayerName = ReplayHandler.RemoveInvalidChars(PlayerName);
                DirectoryFileReplay.Add(new KeyValuePair<string, List<File<IReplay>>>(sortDirectory + @"\" + PlayerName, new List<File<IReplay>>()));
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
                        GetPlayerType(MakeFolderForWinner, MakeFolderForLoser),
                        replay.Content
                    ),
                    DirectoryFileReplay
                );

                if (!(MakeFolderForWinner || MakeFolderForLoser))
                {
                    threwException = !MoveAndRenameReplay(replay, sortDirectory, string.Empty, !IsNested, KeepOriginalReplayNames, Sorter.CustomReplayFormat, DirectoryFileReplay);
                }

                if (threwException)
                    replaysThrowingExceptions.Add(replay.OriginalFilePath);
            }
            return DirectoryFileReplay;
        }

        #endregion

        #endregion

    }
}
