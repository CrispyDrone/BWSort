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
            if (playerType == PlayerType.None)
                return Enumerable.Empty<string>();

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

        private bool MoveOrCopyReplayToPlayerFolders(File<IReplay> replay, IEnumerable<IPlayer> players, IDictionary<string, List<File<IReplay>>> directoryFileReplay, bool isPreview = false)
        {
            bool threwException = false;

            foreach (var player in players)
            {
                if (!MoveAndRenameReplay(
                        replay,
                        Sorter.CurrentDirectory + @"\" + SortCriteria.ToString(),
                        player.Name,
                        !(IsNested == true && player == players.Last()),
                        IsNested ? true : KeepOriginalReplayNames,
                        IsNested ? null : Sorter.CustomReplayFormat,
                        directoryFileReplay,
                        isPreview))
                {
                    threwException = true;
                }
            }

            return !threwException;
        }

        private bool MoveAndRenameReplay(
                File<IReplay> replay, 
                string sortDirectory, 
                string folderName, 
                bool shouldCopy,
                bool keepOriginalReplayNames, 
                CustomReplayFormat customReplayFormat, 
                IDictionary<string, List<File<IReplay>>> directoryFileReplay,
                bool isPreview = false
            )
        {
            bool threwException = false;
            if (!string.IsNullOrWhiteSpace(folderName))
                folderName = FileHandler.RemoveInvalidChars(folderName);

            try
            {
                if (shouldCopy)
                {
                    ReplayHandler.CopyReplay(replay, sortDirectory, folderName, keepOriginalReplayNames, Sorter.CustomReplayFormat, isPreview);
                    var additionalReplayCreated = File<IReplay>.Create(replay.Content, replay.OriginalFilePath, replay.Hash);
                    additionalReplayCreated.AddAfterCurrent(replay.FilePath);
                    replay.Rewind();
                    directoryFileReplay[Path.Combine(sortDirectory, folderName)].Add(additionalReplayCreated);
                }
                else
                {
                    ReplayHandler.MoveReplay(replay, sortDirectory, folderName, keepOriginalReplayNames, Sorter.CustomReplayFormat, isPreview);
                    directoryFileReplay[Path.Combine(sortDirectory, folderName)].Add(replay);
                }
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
            var directoryFileReplay = new Dictionary<string, List<File<IReplay>>>();

            var playerNames = new List<string>();
            var makeFolderForWinner = (bool)SortCriteriaParameters.MakeFolderForWinner;
            var makeFolderForLoser = (bool)SortCriteriaParameters.MakeFolderForLoser;
            var currentDirectory = Sorter.CurrentDirectory;
            var sortCriteria = Sorter.SortCriteria;
            var sortDirectory = Sorter.CurrentDirectory;

            playerNames.AddRange(ExtractPlayers(Sorter.ListReplays.Select(f => f.Content), GetPlayerType(makeFolderForWinner, makeFolderForLoser)).Select(playerName => FileHandler.RemoveInvalidChars(playerName)).Distinct());

            if (!(IsNested && !Sorter.GenerateIntermediateFolders))
            {
                if (IsNested)
                {
                    sortDirectory = Path.Combine(Sorter.CurrentDirectory, sortCriteria.ToString());
                }
                else
                {
                    sortDirectory = Path.Combine(Sorter.CurrentDirectory, string.Join(",", Sorter.CriteriaStringOrder));
                }
                sortDirectory = FileHandler.CreateDirectory(sortDirectory);
            }

            var playerType = GetPlayerType(makeFolderForWinner, makeFolderForLoser);

            foreach (var playerName in playerNames)
            {
                try
                {
                    Directory.CreateDirectory(sortDirectory + @"\" + playerName);
                    directoryFileReplay.Add(Path.Combine(sortDirectory, playerName), new List<File<IReplay>>());
                }
                catch (Exception ex)
                {
                    ErrorLogger.GetInstance()?.LogError($"{DateTime.Now} - Could not create folder for {playerName}", ex: ex);
                }
            }

            if (playerType == PlayerType.None)
                directoryFileReplay.Add(sortDirectory, new List<File<IReplay>>());

            foreach (var replay in Sorter.ListReplays)
            {
                var playersToSortFor = GetPlayers(playerType, replay.Content);
                var newReplayName = KeepOriginalReplayNames ? FileHandler.GetFileName(replay.FilePath) : ReplayHandler.GenerateReplayName(replay, Sorter.CustomReplayFormat) + ".rep";

                foreach (var player in playersToSortFor)
                {
                    try
                    {
                        var folderName = Path.Combine(sortDirectory, playerType == PlayerType.None ? string.Empty : FileHandler.RemoveInvalidChars(player.Name));
                        var newReplayPath = Path.Combine(folderName, newReplayName);

                        if (IsNested && player == playersToSortFor.Last())
                        {
                            ReplayHandler.MoveReplay(replay, newReplayPath);
                        }
                        else
                        {
                            ReplayHandler.CopyReplay(replay, newReplayPath);
                            var additionalReplayCreated = File<IReplay>.Create(replay.Content, replay.OriginalFilePath, replay.Hash);
                            additionalReplayCreated.AddAfterCurrent(replay.FilePath);
                            replay.Rewind();
                            directoryFileReplay[folderName].Add(additionalReplayCreated);
                        }
                        directoryFileReplay[folderName].Add(replay);
                    }
                    catch (Exception ex)
                    {
                        replaysThrowingExceptions.Add(replay.OriginalFilePath);
                        ErrorLogger.GetInstance()?.LogError($"{DateTime.Now} - failed to move replay: {replay.FilePath}", ex: ex);
                    }
                }
            }
            return directoryFileReplay;
        }

        //TODO rewrite
        public IDictionary<string, List<File<IReplay>>> SortAsync(List<string> replaysThrowingExceptions, BackgroundWorker worker_ReplaySorter, int currentCriteria, int numberOfCriteria, int currentPositionNested, int numberOfPositions)
        {
            // Dictionary<directory, dictionary<file, replay>>
            IDictionary<string, List<File<IReplay>>> directoryFileReplay = new Dictionary<string, List<File<IReplay>>>();

            List<string> playerNames = new List<string>();
            bool makeFolderForWinner = (bool)SortCriteriaParameters.MakeFolderForWinner;
            bool makeFolderForLoser = (bool)SortCriteriaParameters.MakeFolderForLoser;
            string CurrentDirectory = Sorter.CurrentDirectory;
            Criteria SortCriteria = Sorter.SortCriteria;

            playerNames.AddRange(ExtractPlayers(Sorter.ListReplays.Select(f => f.Content), GetPlayerType(makeFolderForWinner, makeFolderForLoser)).Select(playerName => FileHandler.RemoveInvalidChars(playerName)).Distinct());

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

            var playerType = GetPlayerType(makeFolderForWinner, makeFolderForLoser);

            foreach (var playerName in playerNames/*.Distinct()*/)
            {
                try
                {
                    Directory.CreateDirectory(sortDirectory + @"\" + playerName);
                    var FileReplays = new List<File<IReplay>>();
                    directoryFileReplay.Add(new KeyValuePair<string, List<File<IReplay>>>(sortDirectory + @"\" + playerName, FileReplays));
                }
                catch (Exception ex)
                {
                    ErrorLogger.GetInstance()?.LogError($"{DateTime.Now} - Could not create folder for {playerName}", ex: ex);
                }
            }

            if (playerType == PlayerType.None)
                directoryFileReplay.Add(sortDirectory, new List<File<IReplay>>());

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
                                threwException = !MoveAndRenameReplay(replay, sortDirectory, PlayerName, false, true, null, directoryFileReplay);
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
                                threwException = !MoveAndRenameReplay(replay, sortDirectory, PlayerName, false, true, null, directoryFileReplay);
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
                                        threwException = !MoveAndRenameReplay(replay, sortDirectory, PlayerName, false, true, null, directoryFileReplay);
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
                                    threwException = !MoveAndRenameReplay(replay, sortDirectory, PlayerName, false, true, null, directoryFileReplay);
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
                            threwException = !MoveAndRenameReplay(replay, sortDirectory, string.Empty, false, true, null, directoryFileReplay);
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
            var directoryFileReplay = new Dictionary<string, List<File<IReplay>>>();

            var playerNames = new List<string>();
            var makeFolderForWinner = (bool)SortCriteriaParameters.MakeFolderForWinner;
            var makeFolderForLoser = (bool)SortCriteriaParameters.MakeFolderForLoser;
            var currentDirectory = Sorter.CurrentDirectory;
            var sortCriteria = Sorter.SortCriteria;
            var playerType = GetPlayerType(makeFolderForWinner, makeFolderForLoser);

            playerNames.AddRange(ExtractPlayers(Sorter.ListReplays.Select(f => f.Content), playerType).Select(playerName => FileHandler.RemoveInvalidChars(playerName)).Distinct());

            string sortDirectory = Sorter.CurrentDirectory;
            if (!(IsNested && !Sorter.GenerateIntermediateFolders))
            {
                if (IsNested)
                {
                    sortDirectory = Sorter.CurrentDirectory + @"\" + sortCriteria;
                }
                else
                {
                    sortDirectory = Sorter.CurrentDirectory + @"\" + string.Join(",", Sorter.CriteriaStringOrder);
                }
                sortDirectory = FileHandler.AdjustName(sortDirectory, true);
            }

            foreach (var playerName in playerNames)
            {
                directoryFileReplay.Add(Path.Combine(sortDirectory, playerName), new List<File<IReplay>>());
            }

            if (playerType == PlayerType.None)
                directoryFileReplay.Add(sortDirectory, new List<File<IReplay>>());

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

                if (!(makeFolderForWinner || makeFolderForLoser))
                {
                    threwException = !MoveAndRenameReplay(replay, sortDirectory, string.Empty, !IsNested, IsNested ? true : KeepOriginalReplayNames, IsNested ? null : Sorter.CustomReplayFormat, directoryFileReplay, true);
                }
                else
                {
                    threwException = !MoveOrCopyReplayToPlayerFolders(
                        replay,
                        GetPlayers(
                            GetPlayerType(makeFolderForWinner, makeFolderForLoser),
                            replay.Content
                        ),
                        directoryFileReplay,
                        true
                    );
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
