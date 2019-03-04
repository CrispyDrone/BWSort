﻿using System;
using System.Collections.Generic;
using System.IO;
using ReplayParser.Interfaces;
using ReplayParser.ReplaySorter.Diagnostics;
using System.ComponentModel;

namespace ReplayParser.ReplaySorter.Sorting.SortCommands
{
    public class SortOnMap : ISortCommand
    {
        public SortOnMap(SortCriteriaParameters sortcriteriaparameters, bool keeporiginalreplaynames, Sorter sorter)
        {
            SortCriteriaParameters = sortcriteriaparameters;
            KeepOriginalReplayNames = keeporiginalreplaynames;
            Sorter = sorter;
        }
        public bool KeepOriginalReplayNames { get; set; }

        public SortCriteriaParameters SortCriteriaParameters { get; set; }
        public Criteria SortCriteria { get { return Criteria.MAP; } }
        public bool IsNested { get; set; }
        public Sorter Sorter { get; set; }

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
            sortDirectory = Sorter.CreateDirectory(sortDirectory);

            foreach (var map in Maps)
            {
                var MapName = map.Key.MapName;
                List<File<IReplay>> FileReplays = new List<File<IReplay>>();

                MapName = ReplayHandler.RemoveInvalidChars(MapName);

                try
                {
                    if (!Directory.Exists(sortDirectory + @"\" + MapName))
                    {
                        Directory.CreateDirectory(sortDirectory + @"\" + MapName);
                    }
                    else
                    {
                        int counter = 1;
                        string TempName = MapName;
                        while (Directory.Exists(sortDirectory + @"\" + TempName))
                        {
                            TempName = IncrementName(MapName, ref counter);
                        }
                        MapName = TempName;
                        Directory.CreateDirectory(sortDirectory + @"\" + MapName);
                    }
                    var MapReplays = Maps[map.Key];
                    foreach (var replay in MapReplays)
                    {
                        bool threwException = false;
                        try
                        {
                            if (IsNested == false)
                            {
                                ReplayHandler.CopyReplay(replay, sortDirectory, MapName, KeepOriginalReplayNames, Sorter.CustomReplayFormat);
                            }
                            else
                            {
                                ReplayHandler.MoveReplay(replay, sortDirectory, MapName, KeepOriginalReplayNames, Sorter.CustomReplayFormat);
                            }

                            FileReplays.Add(replay);
                        }
                        catch (IOException IOex)
                        {
                            threwException = true;
                            ErrorLogger.LogError("SortOnGameType IOException.", Sorter.OriginalDirectory + @"\LogErrors", IOex);
                        }
                        catch (NotSupportedException NSE)
                        {
                            threwException = true;
                            ErrorLogger.LogError("SortOnGameType NotSupportedException.", Sorter.OriginalDirectory + @"\LogErrors", NSE);
                        }
                        catch (NullReferenceException nullex)
                        {
                            threwException = true;
                            ErrorLogger.LogError("SortOnGameType NullReferenceException.", Sorter.OriginalDirectory + @"\LogErrors", nullex);
                        }
                        catch (ArgumentException AEX)
                        {
                            threwException = true;
                            ErrorLogger.LogError("SortOnGameType ArgumentException.", Sorter.OriginalDirectory + @"\LogErrors", AEX);
                        }
                        catch (Exception ex)
                        {
                            threwException = true;
                            ErrorLogger.LogError("SortOnGameType Exception.", Sorter.OriginalDirectory + @"\LogErrors", ex);
                        }
                        if (threwException)
                            replaysThrowingExceptions.Add(replay.OriginalFileName);
                    }
                    // key already exists... how/why?? "Untitled Scenario"... different maps, same "internal" name
                    var MapFolder = sortDirectory + @"\" + MapName;
                    //var TempName = sortDirectory + @"\" + MapName;
                    //int count = 1;
                    //while (DirectoryFileReplay.ContainsKey(TempName))
                    //    TempName = IncrementName(MapFolder, ref count);
                    //MapFolder = TempName;

                    DirectoryFileReplay.Add(new KeyValuePair<string, List<File<IReplay>>>(MapFolder, FileReplays));
                }
                catch (Exception ex)
                {
                    ErrorLogger.LogError($"SortOnGameType Exception Outer: {DirectoryFileReplay.Count.ToString()}.", Sorter.OriginalDirectory + @"\LogErrors", ex);
                    //Console.WriteLine(ex.Message);
                    //Console.WriteLine(DirectoryFileReplay.Count.ToString());
                }
            }
            // not implemented yet
            return DirectoryFileReplay;
        }

        private string IncrementName(string Name, ref int counter)
        {
            return string.Format("{0}({1})", Name, counter++);
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
            sortDirectory = Sorter.CreateDirectory(sortDirectory, true);
            int currentPosition = 0;
            int progressPercentage = 0;

            foreach (var map in Maps)
            {
                var MapName = map.Key.MapName;
                var FileReplays = new List<File<IReplay>>();

                MapName = ReplayHandler.RemoveInvalidChars(MapName);

                try
                {
                    if (!Directory.Exists(sortDirectory + @"\" + MapName))
                    {
                        Directory.CreateDirectory(sortDirectory + @"\" + MapName);
                    }
                    else
                    {
                        int counter = 1;
                        string TempName = MapName;
                        while (Directory.Exists(sortDirectory + @"\" + TempName))
                        {
                            TempName = IncrementName(MapName, ref counter);
                        }
                        MapName = TempName;
                        Directory.CreateDirectory(sortDirectory + @"\" + MapName);
                    }
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
                                ReplayHandler.CopyReplay(replay, sortDirectory, MapName, KeepOriginalReplayNames, Sorter.CustomReplayFormat);
                            }
                            else
                            {
                                ReplayHandler.MoveReplay(replay, sortDirectory, MapName, KeepOriginalReplayNames, Sorter.CustomReplayFormat);
                            }

                            FileReplays.Add(replay);
                        }
                        catch (IOException IOex)
                        {
                            threwException = true;
                            ErrorLogger.LogError("SortOnGameType IOException.", Sorter.OriginalDirectory + @"\LogErrors", IOex);
                        }
                        catch (NotSupportedException NSE)
                        {
                            threwException = true;
                            ErrorLogger.LogError("SortOnGameType NotSupportedException.", Sorter.OriginalDirectory + @"\LogErrors", NSE);
                        }
                        catch (NullReferenceException nullex)
                        {
                            threwException = true;
                            ErrorLogger.LogError("SortOnGameType NullReferenceException.", Sorter.OriginalDirectory + @"\LogErrors", nullex);
                        }
                        catch (ArgumentException AEX)
                        {
                            threwException = true;
                            ErrorLogger.LogError("SortOnGameType ArgumentException.", Sorter.OriginalDirectory + @"\LogErrors", AEX);
                        }
                        catch (Exception ex)
                        {
                            threwException = true;
                            ErrorLogger.LogError("SortOnGameType Exception.", Sorter.OriginalDirectory + @"\LogErrors", ex);
                        }
                        if (threwException)
                            replaysThrowingExceptions.Add(replay.OriginalFileName);
                    }
                    // key already exists... how/why?? "Untitled Scenario"... different maps, same "internal" name
                    var MapFolder = sortDirectory + @"\" + MapName;

                    DirectoryFileReplay.Add(new KeyValuePair<string, List<File<IReplay>>>(MapFolder, FileReplays));
                }
                catch (Exception ex)
                {
                    ErrorLogger.LogError($"SortOnGameType Exception Outer: {DirectoryFileReplay.Count.ToString()}.", Sorter.OriginalDirectory + @"\LogErrors", ex);
                }
            }
            // not implemented yet
            return DirectoryFileReplay;
        }
    }
}
