using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

        public IDictionary<string, IDictionary<string, IReplay>> Sort()
        {
            // Dictionary<directory, dictionary<file, replay>>
            IDictionary<string, IDictionary<string, IReplay>> DirectoryFileReplay = new Dictionary<string, IDictionary<string, IReplay>>();

            // extract maps from replays, try to group the duplicates
            ReplayMapEqualityComparer MapEq = new ReplayMapEqualityComparer();
            IDictionary<IReplayMap, List<IReplay>> Maps = new Dictionary<IReplayMap, List<IReplay>>(MapEq);


            foreach (var replay in Sorter.ListReplays)
            {
                if (!Maps.Keys.Contains(replay.ReplayMap))
                {
                    Maps.Add(new KeyValuePair<IReplayMap, List<IReplay>>(replay.ReplayMap, new List<IReplay> { replay }));
                }
                else
                {
                    Maps[replay.ReplayMap].Add(replay);
                }
            }

            string sortDirectory = Sorter.CurrentDirectory + @"\" + Sorter.SortCriteria.ToString();
            sortDirectory = Sorter.CreateDirectory(sortDirectory);

            foreach (var map in Maps)
            {
                var MapName = map.Key.MapName;
                IDictionary<string, IReplay> FileReplays = new Dictionary<string, IReplay>();

                MapName = Sorter.RemoveInvalidChars(MapName);

                //foreach (char invalidChar in Sorter.InvalidFileChars)
                //{
                //    MapName = MapName.Replace(invalidChar.ToString(), string.Empty);
                //}
                //foreach (char invalidChar in Sorter.InvalidFileCharsAdditional)
                //{
                //    MapName = MapName.Replace(invalidChar.ToString(), string.Empty);
                //}

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
                        try
                        {
                            string File = string.Empty;
                            if (IsNested == false)
                            {
                                File = ReplayHandler.CopyReplay(Sorter.ListReplays, replay, Sorter.Files, sortDirectory, MapName, KeepOriginalReplayNames, Sorter.CustomReplayFormat);
                            }
                            else
                            {
                                File = ReplayHandler.MoveReplay(Sorter.ListReplays, replay, Sorter.Files, sortDirectory, MapName, KeepOriginalReplayNames, Sorter.CustomReplayFormat);
                            }

                            FileReplays.Add(new KeyValuePair<string, IReplay>(/*Sorter.Files.ElementAt(Sorter.ListReplays.IndexOf(replay))*/File, replay));
                        }
                        catch (IOException IOex)
                        {
                            ErrorLogger.LogError("SortOnGameType IOException.", Sorter.OriginalDirectory + @"\LogErrors", IOex);
                            //Console.WriteLine(IOex.Message);
                        }
                        catch (NotSupportedException NSE)
                        {
                            ErrorLogger.LogError("SortOnGameType NotSupportedException.", Sorter.OriginalDirectory + @"\LogErrors", NSE);
                            Console.WriteLine(NSE.Message);
                        }
                        catch (NullReferenceException nullex)
                        {
                            ErrorLogger.LogError("SortOnGameType NullReferenceException.", Sorter.OriginalDirectory + @"\LogErrors", nullex);
                            Console.WriteLine(nullex.Message);
                        }
                        catch (ArgumentException AEX)
                        {
                            ErrorLogger.LogError("SortOnGameType ArgumentException.", Sorter.OriginalDirectory + @"\LogErrors", AEX);
                            Console.WriteLine(AEX.Message);
                        }
                        catch (Exception ex)
                        {
                            ErrorLogger.LogError("SortOnGameType Exception.", Sorter.OriginalDirectory + @"\LogErrors", ex);
                            Console.WriteLine(ex.Message);
                        }
                    }
                    // key already exists... how/why?? "Untitled Scenario"... different maps, same "internal" name
                    var MapFolder = sortDirectory + @"\" + MapName;
                    //var TempName = sortDirectory + @"\" + MapName;
                    //int count = 1;
                    //while (DirectoryFileReplay.ContainsKey(TempName))
                    //    TempName = IncrementName(MapFolder, ref count);
                    //MapFolder = TempName;

                    DirectoryFileReplay.Add(new KeyValuePair<string, IDictionary<string, IReplay>>(MapFolder, FileReplays));
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

        public IDictionary<string, IDictionary<string, IReplay>> SortAsync(BackgroundWorker worker_ReplaySorter, int currentCriteria, int numberOfCriteria, int currentPositionNested, int numberOfPositions)
        {
            // Dictionary<directory, dictionary<file, replay>>
            IDictionary<string, IDictionary<string, IReplay>> DirectoryFileReplay = new Dictionary<string, IDictionary<string, IReplay>>();

            // extract maps from replays, try to group the duplicates
            ReplayMapEqualityComparer MapEq = new ReplayMapEqualityComparer();
            IDictionary<IReplayMap, List<IReplay>> Maps = new Dictionary<IReplayMap, List<IReplay>>(MapEq);


            foreach (var replay in Sorter.ListReplays)
            {
                if (!Maps.Keys.Contains(replay.ReplayMap))
                {
                    Maps.Add(new KeyValuePair<IReplayMap, List<IReplay>>(replay.ReplayMap, new List<IReplay> { replay }));
                }
                else
                {
                    Maps[replay.ReplayMap].Add(replay);
                }
            }

            string sortDirectory = Sorter.CurrentDirectory + @"\" + Sorter.SortCriteria.ToString();
            sortDirectory = Sorter.CreateDirectory(sortDirectory, true);
            int currentPosition = 0;
            int progressPercentage = 0;

            foreach (var map in Maps)
            {
                var MapName = map.Key.MapName;
                IDictionary<string, IReplay> FileReplays = new Dictionary<string, IReplay>();

                MapName = Sorter.RemoveInvalidChars(MapName);

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
                        worker_ReplaySorter.ReportProgress(progressPercentage, "sorting on map...");

                        try
                        {
                            string File = string.Empty;
                            if (IsNested == false)
                            {
                                File = ReplayHandler.CopyReplay(Sorter.ListReplays, replay, Sorter.Files, sortDirectory, MapName, KeepOriginalReplayNames, Sorter.CustomReplayFormat);
                            }
                            else
                            {
                                File = ReplayHandler.MoveReplay(Sorter.ListReplays, replay, Sorter.Files, sortDirectory, MapName, KeepOriginalReplayNames, Sorter.CustomReplayFormat);
                            }

                            FileReplays.Add(new KeyValuePair<string, IReplay>(File, replay));
                        }
                        catch (IOException IOex)
                        {
                            ErrorLogger.LogError("SortOnGameType IOException.", Sorter.OriginalDirectory + @"\LogErrors", IOex);
                        }
                        catch (NotSupportedException NSE)
                        {
                            ErrorLogger.LogError("SortOnGameType NotSupportedException.", Sorter.OriginalDirectory + @"\LogErrors", NSE);
                            Console.WriteLine(NSE.Message);
                        }
                        catch (NullReferenceException nullex)
                        {
                            ErrorLogger.LogError("SortOnGameType NullReferenceException.", Sorter.OriginalDirectory + @"\LogErrors", nullex);
                            Console.WriteLine(nullex.Message);
                        }
                        catch (ArgumentException AEX)
                        {
                            ErrorLogger.LogError("SortOnGameType ArgumentException.", Sorter.OriginalDirectory + @"\LogErrors", AEX);
                            Console.WriteLine(AEX.Message);
                        }
                        catch (Exception ex)
                        {
                            ErrorLogger.LogError("SortOnGameType Exception.", Sorter.OriginalDirectory + @"\LogErrors", ex);
                            Console.WriteLine(ex.Message);
                        }
                    }
                    // key already exists... how/why?? "Untitled Scenario"... different maps, same "internal" name
                    var MapFolder = sortDirectory + @"\" + MapName;

                    DirectoryFileReplay.Add(new KeyValuePair<string, IDictionary<string, IReplay>>(MapFolder, FileReplays));
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
