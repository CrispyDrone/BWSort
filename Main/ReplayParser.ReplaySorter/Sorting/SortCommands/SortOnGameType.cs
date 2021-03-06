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
    public class SortOnGameType : ISortCommand
    {
        #region private

        #region methods

        #endregion

        #endregion

        #region public

        #region constructor

        public SortOnGameType(SortCriteriaParameters sortcriteriaparameters, bool keeporiginalreplaynames, Sorter sorter)
        {
            SortCriteriaParameters = sortcriteriaparameters;
            KeepOriginalReplayNames = keeporiginalreplaynames;
            Sorter = sorter;
        }

        #endregion

        #region properties

        public bool KeepOriginalReplayNames { get; set; }

        public SortCriteriaParameters SortCriteriaParameters { get; set; }
        public Criteria SortCriteria { get { return Criteria.GAMETYPE; } }
        public bool IsNested { get; set; }
        public Sorter Sorter { get; set; }

        #endregion

        #region methods

        public IDictionary<string, List<File<IReplay>>> Sort(List<string> replaysThrowingExceptions)
        {
            // Dictionary<directory, dictionary<file, replay>>
            IDictionary<string, List<File<IReplay>>> DirectoryFileReplay = new Dictionary<string, List<File<IReplay>>>();

            // replays grouped by gametype
            var ReplaysByGameTypes = from replay in Sorter.ListReplays
                                     group replay by replay.Content.GameType;

            // make sortdirectory
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

            // make subdirectory per gametype, and put all associated replays into it

            foreach (var gametype in ReplaysByGameTypes)
            {
                var GameType = gametype.Key.ToString();
                Directory.CreateDirectory(sortDirectory + @"\" + GameType);
                var FileReplays = new List<File<IReplay>>();
                DirectoryFileReplay.Add(new KeyValuePair<string, List<File<IReplay>>>(sortDirectory + @"\" + GameType, FileReplays));

                foreach (var replay in gametype)
                {
                    try
                    {
                        if (IsNested == false)
                        {
                            ReplayHandler.CopyReplay(replay, sortDirectory, GameType, KeepOriginalReplayNames, Sorter.CustomReplayFormat);
                        }
                        else
                        {
                            ReplayHandler.MoveReplay(replay, sortDirectory, GameType, true, null);
                        }
                        
                        FileReplays.Add(replay);
                    }
                    catch (Exception ex)
                    {
                        replaysThrowingExceptions.Add(replay.OriginalFilePath);
                        ErrorLogger.GetInstance()?.LogError($"{DateTime.Now} - SortOnGameType exception.", ex: ex);
                    }
                }
            }
            return DirectoryFileReplay;
        }

        public IDictionary<string, List<File<IReplay>>> SortAsync(List<string> replaysThrowingExceptions, BackgroundWorker worker_ReplaySorter, int currentCriteria, int numberOfCriteria, int currentPositionNested, int numberOfPositions)
        {
            // Dictionary<directory, dictionary<file, replay>>
            IDictionary<string, List<File<IReplay>>> DirectoryFileReplay = new Dictionary<string, List<File<IReplay>>>();

            // replays grouped by gametype
            var ReplaysByGameTypes = from replay in Sorter.ListReplays
                                     group replay by replay.Content.GameType;

            // make sortdirectory
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

            // make subdirectory per gametype, and put all associated replays into it

            int currentPosition = 0;
            int progressPercentage = 0;
            foreach (var gametype in ReplaysByGameTypes)
            {
                var GameType = gametype.Key.ToString();
                Directory.CreateDirectory(sortDirectory + @"\" + GameType);
                var FileReplays = new List<File<IReplay>>();
                DirectoryFileReplay.Add(new KeyValuePair<string, List<File<IReplay>>>(sortDirectory + @"\" + GameType, FileReplays));

                foreach (var replay in gametype)
                {
                    if (worker_ReplaySorter.CancellationPending == true)
                    {
                        return null;
                    }
                    try
                    {
                        if (IsNested == false)
                        {
                            ReplayHandler.CopyReplay(replay, sortDirectory, GameType, KeepOriginalReplayNames, Sorter.CustomReplayFormat);
                        }
                        else
                        {
                            ReplayHandler.MoveReplay(replay, sortDirectory, GameType, true, null);
                        }

                        FileReplays.Add(replay);
                    }
                    catch (Exception ex)
                    {
                        replaysThrowingExceptions.Add(replay.OriginalFilePath);
                        ErrorLogger.GetInstance()?.LogError("SortOnGameType ArgumentException.", ex: ex);
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
                    worker_ReplaySorter.ReportProgress(progressPercentage, $"sorting on gametype... {replay.FilePath}");
                }
            }
            return DirectoryFileReplay;
        }

        public IDictionary<string, List<File<IReplay>>> PreviewSort(List<string> replaysThrowingExceptions, BackgroundWorker worker_ReplaySorter, int currentCriteria, int numberOfCriteria, int currentPositionNested = 0, int numberOfPositions = 0)
        {
            IDictionary<string, List<File<IReplay>>> DirectoryFileReplay = new Dictionary<string, List<File<IReplay>>>();

            var ReplaysByGameTypes = from replay in Sorter.ListReplays
                                     group replay by replay.Content.GameType;

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

            int currentPosition = 0;
            int progressPercentage = 0;
            foreach (var gametype in ReplaysByGameTypes)
            {
                var GameType = gametype.Key.ToString();
                var FileReplays = new List<File<IReplay>>();
                DirectoryFileReplay.Add(new KeyValuePair<string, List<File<IReplay>>>(sortDirectory + @"\" + GameType, FileReplays));

                foreach (var replay in gametype)
                {
                    if (worker_ReplaySorter.CancellationPending == true)
                    {
                        return null;
                    }

                    try
                    {
                        if (IsNested == false)
                        {
                            ReplayHandler.CopyReplay(replay, sortDirectory, GameType, KeepOriginalReplayNames, Sorter.CustomReplayFormat, true);
                        }
                        else
                        {
                            ReplayHandler.MoveReplay(replay, sortDirectory, GameType, true, null, true);
                        }

                        FileReplays.Add(replay);
                    }
                    catch (Exception ex)
                    {
                        replaysThrowingExceptions.Add(replay.OriginalFilePath);
                        ErrorLogger.GetInstance()?.LogError("SortOnGameType ArgumentException.", ex: ex);
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
                    worker_ReplaySorter.ReportProgress(progressPercentage, $"sorting on gametype... {replay.FilePath}");
                }
            }
            return DirectoryFileReplay;
        }

        #endregion

        #endregion
    }
}
