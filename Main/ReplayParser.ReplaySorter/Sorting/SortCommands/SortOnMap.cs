using System;
using System.Collections.Generic;
using System.IO;
using ReplayParser.Interfaces;
using ReplayParser.ReplaySorter.Diagnostics;
using System.ComponentModel;
using ReplayParser.ReplaySorter.IO;

namespace ReplayParser.ReplaySorter.Sorting.SortCommands
{
    public class SortOnMap : ISortCommand
    {
        #region private

        #region methods

        private string IncrementName(string Name, ref int counter)
        {
            return string.Format("{0}({1})", Name, counter++);
        }

        #endregion

        #endregion

        #region public

        #region constructor

        public SortOnMap(SortCriteriaParameters sortcriteriaparameters, bool keeporiginalreplaynames, Sorter sorter)
        {
            SortCriteriaParameters = sortcriteriaparameters;
            KeepOriginalReplayNames = keeporiginalreplaynames;
            Sorter = sorter;
        }

        #endregion

        #region properties

        public bool KeepOriginalReplayNames { get; set; }
        public SortCriteriaParameters SortCriteriaParameters { get; set; }
        public Criteria SortCriteria { get { return Criteria.MAP; } }
        public bool IsNested { get; set; }
        public Sorter Sorter { get; set; }

        #endregion

        #region methods

        public IDictionary<string, List<File<IReplay>>> Sort(List<string> replaysThrowingExceptions)
        {
            // Dictionary<directory, dictionary<file, replay>>
            IDictionary<string, List<File<IReplay>>> DirectoryFileReplay = new Dictionary<string, List<File<IReplay>>>();

            // extract maps from replays, try to group the duplicates
            ReplayMapEqualityComparer MapEq = new ReplayMapEqualityComparer();
            IDictionary<IReplayMap, List<File<IReplay>>> Maps = new Dictionary<IReplayMap, List<File<IReplay>>>(MapEq);


            foreach (var replay in Sorter.ListReplays)
            {
                if (!Maps.Keys.Contains(replay.Content.ReplayMap))
                {
                    Maps.Add(new KeyValuePair<IReplayMap, List<File<IReplay>>>(replay.Content.ReplayMap, new List<File<IReplay>> { replay }));
                }
                else
                {
                    Maps[replay.Content.ReplayMap].Add(replay);
                }
            }

            string sortDirectory = Sorter.CurrentDirectory + @"\" + Sorter.SortCriteria.ToString();
            sortDirectory = FileHandler.CreateDirectory(sortDirectory);

            foreach (var map in Maps)
            {
                var MapName = map.Key.MapName;
                List<File<IReplay>> FileReplays = new List<File<IReplay>>();

                MapName = ReplayHandler.RemoveInvalidChars(MapName);

                try
                {
                    //TODO use stringbuilder ? 
                    string MapFolder = sortDirectory + @"\" + MapName;

                    // key already exists... how/why?? "Untitled Scenario"... different maps, same "internal" name
                    // maps with same name but different dimensions...

                    int count = 1;
                    while (DirectoryFileReplay.ContainsKey(MapFolder) || Directory.Exists(MapFolder))
                    {
                        MapFolder = FileHandler.IncrementName(MapName, string.Empty, sortDirectory, ref count);
                    }

                    DirectoryFileReplay.Add(new KeyValuePair<string, List<File<IReplay>>>(MapFolder, FileReplays));

                    Directory.CreateDirectory(MapFolder);

                    var MapReplays = Maps[map.Key];
                    foreach (var replay in MapReplays)
                    {
                        bool threwException = false;
                        try
                        {
                            if (IsNested == false)
                            {
                                ReplayHandler.CopyReplay(replay, MapFolder, string.Empty, KeepOriginalReplayNames, Sorter.CustomReplayFormat);
                            }
                            else
                            {
                                ReplayHandler.MoveReplay(replay, MapFolder, string.Empty, KeepOriginalReplayNames, Sorter.CustomReplayFormat);
                            }

                            FileReplays.Add(replay);
                        }
                        catch (IOException IOex)
                        {
                            threwException = true;
                            ErrorLogger.GetInstance()?.LogError("SortOnGameType IOException.", ex: IOex);
                        }
                        catch (NotSupportedException NSE)
                        {
                            threwException = true;
                            ErrorLogger.GetInstance()?.LogError("SortOnGameType NotSupportedException.", ex: NSE);
                        }
                        catch (NullReferenceException nullex)
                        {
                            threwException = true;
                            ErrorLogger.GetInstance()?.LogError("SortOnGameType NullReferenceException.", ex: nullex);
                        }
                        catch (ArgumentException AEX)
                        {
                            threwException = true;
                            ErrorLogger.GetInstance()?.LogError("SortOnGameType ArgumentException.", ex: AEX);
                        }
                        catch (Exception ex)
                        {
                            threwException = true;
                            ErrorLogger.GetInstance()?.LogError("SortOnGameType Exception.", ex: ex);
                        }
                        if (threwException)
                            replaysThrowingExceptions.Add(replay.OriginalFilePath);
                    }
                }
                catch (Exception ex)
                {
                    ErrorLogger.GetInstance()?.LogError($"{DateTime.Now} - SortOnGameType Exception Outer: {DirectoryFileReplay.Count.ToString()}.", ex: ex);
                }
            }
            return DirectoryFileReplay;
        }

        public IDictionary<string, List<File<IReplay>>> SortAsync(List<string> replaysThrowingExceptions, BackgroundWorker worker_ReplaySorter, int currentCriteria, int numberOfCriteria, int currentPositionNested, int numberOfPositions)
        {
            // Dictionary<directory, dictionary<file, replay>>
            IDictionary<string, List<File<IReplay>>> DirectoryFileReplay = new Dictionary<string, List<File<IReplay>>>();

            // extract maps from replays, try to group the duplicates
            ReplayMapEqualityComparer MapEq = new ReplayMapEqualityComparer();
            IDictionary<IReplayMap, List<File<IReplay>>> Maps = new Dictionary<IReplayMap, List<File<IReplay>>>(MapEq);


            foreach (var replay in Sorter.ListReplays)
            {
                if (!Maps.Keys.Contains(replay.Content.ReplayMap))
                {
                    Maps.Add(new KeyValuePair<IReplayMap, List<File<IReplay>>>(replay.Content.ReplayMap, new List<File<IReplay>> { replay }));
                }
                else
                {
                    Maps[replay.Content.ReplayMap].Add(replay);
                }
            }

            string sortDirectory = Sorter.CurrentDirectory + @"\" + Sorter.SortCriteria.ToString();
            sortDirectory = FileHandler.CreateDirectory(sortDirectory, true);
            int currentPosition = 0;
            int progressPercentage = 0;

            foreach (var map in Maps)
            {
                var MapName = map.Key.MapName;
                var FileReplays = new List<File<IReplay>>();

                MapName = ReplayHandler.RemoveInvalidChars(MapName);

                try
                {
                    //TODO use stringbuilder ? 
                    string MapFolder = sortDirectory + @"\" + MapName;

                    // key already exists... how/why?? "Untitled Scenario"... different maps, same "internal" name
                    // maps with same name but different dimensions...

                    int count = 1;
                    while (DirectoryFileReplay.ContainsKey(MapFolder) || Directory.Exists(MapFolder))
                    {
                        MapFolder = FileHandler.IncrementName(MapName, string.Empty, sortDirectory, ref count);
                    }

                    DirectoryFileReplay.Add(new KeyValuePair<string, List<File<IReplay>>>(MapFolder, FileReplays));

                    Directory.CreateDirectory(MapFolder);

                    var MapReplays = Maps[map.Key];
                    foreach (var replay in MapReplays)
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
                        worker_ReplaySorter.ReportProgress(progressPercentage, "sorting on map...");

                        try
                        {
                            if (IsNested == false)
                            {
                                ReplayHandler.CopyReplay(replay, MapFolder, string.Empty, KeepOriginalReplayNames, Sorter.CustomReplayFormat);
                            }
                            else
                            {
                                ReplayHandler.MoveReplay(replay, MapFolder, string.Empty, KeepOriginalReplayNames, Sorter.CustomReplayFormat);
                            }

                            FileReplays.Add(replay);
                        }
                        catch (IOException IOex)
                        {
                            threwException = true;
                            ErrorLogger.GetInstance()?.LogError("SortOnGameType IOException.", ex: IOex);
                        }
                        catch (NotSupportedException NSE)
                        {
                            threwException = true;
                            ErrorLogger.GetInstance()?.LogError("SortOnGameType NotSupportedException.", ex: NSE);
                        }
                        catch (NullReferenceException nullex)
                        {
                            threwException = true;
                            ErrorLogger.GetInstance()?.LogError("SortOnGameType NullReferenceException.", ex: nullex);
                        }
                        catch (ArgumentException AEX)
                        {
                            threwException = true;
                            ErrorLogger.GetInstance()?.LogError("SortOnGameType ArgumentException.", ex: AEX);
                        }
                        catch (Exception ex)
                        {
                            threwException = true;
                            ErrorLogger.GetInstance()?.LogError("SortOnGameType Exception.", ex: ex);
                        }
                        if (threwException)
                            replaysThrowingExceptions.Add(replay.OriginalFilePath);
                    }
                }
                catch (Exception ex)
                {
                    ErrorLogger.GetInstance()?.LogError($"{DateTime.Now} - SortOnGameType Exception Outer: {DirectoryFileReplay.Count.ToString()}.", ex: ex);
                }
            }
            return DirectoryFileReplay;
        }

        public IDictionary<string, List<File<IReplay>>> PreviewSort(List<string> replaysThrowingExceptions, BackgroundWorker worker_ReplaySorter, int currentCriteria, int numberOfCriteria, int currentPositionNested = 0, int numberOfPositions = 0)
        {
            IDictionary<string, List<File<IReplay>>> DirectoryFileReplay = new Dictionary<string, List<File<IReplay>>>();

            ReplayMapEqualityComparer MapEq = new ReplayMapEqualityComparer();
            IDictionary<IReplayMap, List<File<IReplay>>> Maps = new Dictionary<IReplayMap, List<File<IReplay>>>(MapEq);

            foreach (var replay in Sorter.ListReplays)
            {
                if (!Maps.Keys.Contains(replay.Content.ReplayMap))
                {
                    Maps.Add(new KeyValuePair<IReplayMap, List<File<IReplay>>>(replay.Content.ReplayMap, new List<File<IReplay>> { replay }));
                }
                else
                {
                    Maps[replay.Content.ReplayMap].Add(replay);
                }
            }

            string sortDirectory = Sorter.CurrentDirectory + @"\" + Sorter.SortCriteria.ToString();
            sortDirectory = FileHandler.AdjustName(sortDirectory, true);
            int currentPosition = 0;
            int progressPercentage = 0;

            foreach (var map in Maps)
            {
                var MapName = map.Key.MapName;
                var FileReplays = new List<File<IReplay>>();

                MapName = ReplayHandler.RemoveInvalidChars(MapName);

                try
                {
                    //TODO use stringbuilder ? 
                    string MapFolder = sortDirectory + @"\" + MapName;

                    // key already exists... how/why?? "Untitled Scenario"... different maps, same "internal" name
                    // maps with same name but different dimensions...

                    int count = 1;
                    while (DirectoryFileReplay.ContainsKey(MapFolder) || Directory.Exists(MapFolder))
                    {
                        MapFolder = FileHandler.IncrementName(MapName, string.Empty, sortDirectory, ref count);
                    }

                    DirectoryFileReplay.Add(new KeyValuePair<string, List<File<IReplay>>>(MapFolder, FileReplays));

                    var MapReplays = Maps[map.Key];
                    foreach (var replay in MapReplays)
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
                        worker_ReplaySorter.ReportProgress(progressPercentage, "sorting on map...");

                        try
                        {
                            if (IsNested == false)
                            {
                                ReplayHandler.CopyReplay(replay, MapFolder, string.Empty, KeepOriginalReplayNames, Sorter.CustomReplayFormat, true);
                            }
                            else
                            {
                                ReplayHandler.MoveReplay(replay, MapFolder, string.Empty, KeepOriginalReplayNames, Sorter.CustomReplayFormat, true);
                            }

                            FileReplays.Add(replay);
                        }
                        catch (IOException IOex)
                        {
                            threwException = true;
                            ErrorLogger.GetInstance()?.LogError("SortOnGameType IOException.", ex: IOex);
                        }
                        catch (NotSupportedException NSE)
                        {
                            threwException = true;
                            ErrorLogger.GetInstance()?.LogError("SortOnGameType NotSupportedException.", ex: NSE);
                        }
                        catch (NullReferenceException nullex)
                        {
                            threwException = true;
                            ErrorLogger.GetInstance()?.LogError("SortOnGameType NullReferenceException.", ex: nullex);
                        }
                        catch (ArgumentException AEX)
                        {
                            threwException = true;
                            ErrorLogger.GetInstance()?.LogError("SortOnGameType ArgumentException.", ex: AEX);
                        }
                        catch (Exception ex)
                        {
                            threwException = true;
                            ErrorLogger.GetInstance()?.LogError("SortOnGameType Exception.", ex: ex);
                        }
                        if (threwException)
                            replaysThrowingExceptions.Add(replay.OriginalFilePath);
                    }
                }
                catch (Exception ex)
                {
                    ErrorLogger.GetInstance()?.LogError($"{DateTime.Now} - SortOnGameType Exception Outer: {DirectoryFileReplay.Count.ToString()}.", ex: ex);
                }
            }
            return DirectoryFileReplay;
        }

        #endregion

        #endregion
    }
}
