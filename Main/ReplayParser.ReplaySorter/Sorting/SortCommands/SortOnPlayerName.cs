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
                    sortDirectory = Path.Combine(sortDirectory, sortCriteria.ToString());
                }
                else
                {
                    sortDirectory = Path.Combine(sortDirectory, string.Join(",", Sorter.CriteriaStringOrder));
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
                try
                {
                    var newReplayName = KeepOriginalReplayNames ? FileHandler.GetFileName(replay.FilePath) : ReplayHandler.GenerateReplayName(replay, Sorter.CustomReplayFormat) + ".rep";
                    if (playerType == PlayerType.None)
                    {
                        var newReplayPath = Path.Combine(sortDirectory, newReplayName);

                        if (IsNested)
                        {
                            ReplayHandler.MoveReplay(replay, newReplayPath);
                        }
                        else
                        {
                            ReplayHandler.CopyReplay(replay, newReplayPath);
                        }
                        directoryFileReplay[sortDirectory].Add(replay);
                    }
                    else
                    {
                        var playersToSortFor = GetPlayers(playerType, replay.Content);
                        foreach (var player in playersToSortFor)
                        {
                            var folderName = Path.Combine(sortDirectory, FileHandler.RemoveInvalidChars(player.Name));
                            var newReplayPath = Path.Combine(folderName, newReplayName);

                            if (IsNested && player == playersToSortFor.Last())
                            {
                                ReplayHandler.MoveReplay(replay, newReplayPath);
                                directoryFileReplay[folderName].Add(replay);
                            }
                            else
                            {
                                ReplayHandler.CopyReplay(replay, newReplayPath);
                                var additionalReplayCreated = File<IReplay>.Create(replay.Content, replay.OriginalFilePath, replay.Hash);
                                additionalReplayCreated.AddAfterCurrent(replay.FilePath);
                                replay.Rewind();
                                directoryFileReplay[folderName].Add(additionalReplayCreated);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    replaysThrowingExceptions.Add(replay.OriginalFilePath);
                    ErrorLogger.GetInstance()?.LogError($"{DateTime.Now} - sort on player name exception: {replay.FilePath}", ex: ex);
                }
            }
            return directoryFileReplay;
        }

        public IDictionary<string, List<File<IReplay>>> SortAsync(List<string> replaysThrowingExceptions, BackgroundWorker worker_ReplaySorter, int currentCriteria, int numberOfCriteria, int currentPositionNested, int numberOfPositions)
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
                    sortDirectory = Path.Combine(sortDirectory, sortCriteria.ToString());
                }
                else
                {
                    sortDirectory = Path.Combine(sortDirectory, string.Join(",", Sorter.CriteriaStringOrder));
                }
                sortDirectory = FileHandler.CreateDirectory(sortDirectory, true);
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

            int currentPosition = 0;
            int progressPercentage = 0;

            foreach (var replay in Sorter.ListReplays)
            {
                if (worker_ReplaySorter.CancellationPending == true)
                    return null;

                currentPosition++;
                if (IsNested == false)
                {
                    progressPercentage = Convert.ToInt32(((double)currentPosition / Sorter.ListReplays.Count) * 1 / numberOfCriteria * 100);
                }
                else
                {
                    progressPercentage = Convert.ToInt32(
                            (((double)currentPosition / Sorter.ListReplays.Count) * 1 / numberOfPositions * 100 + ((currentPositionNested - 1) * 100 / numberOfPositions))
                                *
                            ((double)1 / numberOfCriteria)
                    );
                    progressPercentage += (currentCriteria - 1) * 100 / numberOfCriteria;
                }
                worker_ReplaySorter.ReportProgress(progressPercentage, $"sorting on playername... {replay.FilePath}");

                var newReplayName = KeepOriginalReplayNames ? FileHandler.GetFileName(replay.FilePath) : ReplayHandler.GenerateReplayName(replay, Sorter.CustomReplayFormat) + ".rep";

                try
                {
                    if (playerType == PlayerType.None)
                    {
                        var newReplayPath = Path.Combine(sortDirectory, newReplayName);

                        if (IsNested)
                        {
                            ReplayHandler.MoveReplay(replay, newReplayPath);
                        }
                        else
                        {
                            ReplayHandler.CopyReplay(replay, newReplayPath);
                        }
                        directoryFileReplay[sortDirectory].Add(replay);
                    }
                    else
                    {
                        var playersToSortFor = GetPlayers(playerType, replay.Content);

                        foreach (var player in playersToSortFor)
                        {
                            var folderName = Path.Combine(sortDirectory, FileHandler.RemoveInvalidChars(player.Name));
                            var newReplayPath = Path.Combine(folderName, newReplayName);

                            if (IsNested && player == playersToSortFor.Last())
                            {
                                ReplayHandler.MoveReplay(replay, newReplayPath);
                                directoryFileReplay[folderName].Add(replay);
                            }
                            else
                            {
                                ReplayHandler.CopyReplay(replay, newReplayPath);
                                var additionalReplayCreated = File<IReplay>.Create(replay.Content, replay.OriginalFilePath, replay.Hash);
                                additionalReplayCreated.AddAfterCurrent(replay.FilePath);
                                replay.Rewind();
                                directoryFileReplay[folderName].Add(additionalReplayCreated);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    replaysThrowingExceptions.Add(replay.OriginalFilePath);
                    ErrorLogger.GetInstance()?.LogError($"{DateTime.Now} - sort on playername exception: {replay.FilePath}", ex: ex);
                }
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
                worker_ReplaySorter.ReportProgress(progressPercentage, $"sorting on playername... {replay.FilePath}");

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
