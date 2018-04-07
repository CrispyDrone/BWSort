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
    public class SortOnDuration : ISortCommand
    {
        public SortOnDuration(SortCriteriaParameters sortcriteriaparamaters, bool keeporiginalreplaynames, Sorter sorter)
        {
            SortCriteriaParameters = sortcriteriaparamaters;
            KeepOriginalReplayNames = keeporiginalreplaynames;
            Sorter = sorter;
            
        }
        public bool KeepOriginalReplayNames { get; set; }
        public SortCriteriaParameters SortCriteriaParameters { get; set; }
        public Criteria SortCriteria { get { return Criteria.DURATION; } }
        public bool IsNested { get; set; }
        public Sorter Sorter { get; set; }

        public IDictionary<string, IDictionary<string, IReplay>> Sort()
        {
            if (SortCriteriaParameters.Durations == null)
            {
                throw new ArgumentException("Duration intervals cannot be null");
            }
            // Dictionary<directory, dictionary<file, replay>>
            IDictionary<string, IDictionary<string, IReplay>> DirectoryFileReplay = new Dictionary<string, IDictionary<string, IReplay>>();

            IDictionary<int, List<IReplay>> ReplayDurations = new Dictionary<int, List<IReplay>>();

            foreach (var replay in Sorter.ListReplays)
            {
                TimeSpan replayDuration = TimeSpan.FromSeconds((replay.FrameCount / ((double)1000 / 42)));
                double replayDurationInMinutes = replayDuration.TotalMinutes;
                int durationInterval = 0;
                while (replayDurationInMinutes > SortCriteriaParameters.Durations[durationInterval])
                {
                    durationInterval++;
                    if (durationInterval == SortCriteriaParameters.Durations.Length)
                    {
                        break;
                    }
                }

                if (durationInterval != SortCriteriaParameters.Durations.Length)
                {
                    if (!ReplayDurations.ContainsKey(SortCriteriaParameters.Durations[durationInterval]))
                    {
                        ReplayDurations.Add(new KeyValuePair<int, List<IReplay>>(SortCriteriaParameters.Durations[durationInterval], new List<IReplay> { replay }));
                    }
                    else
                    {
                        ReplayDurations[SortCriteriaParameters.Durations[durationInterval]].Add(replay);
                    }
                    // => throws error key does not exist !!! => ReplayDurations[durations[durationInterval]].Add(replay);
                }
                else
                {
                    if (!ReplayDurations.ContainsKey(-1))
                    {
                        ReplayDurations.Add(new KeyValuePair<int, List<IReplay>>(-1, new List<IReplay> { replay }));
                    }
                    else
                    {
                        ReplayDurations[-1].Add(replay);
                    }

                }
            }

            string sortDirectory = Sorter.CurrentDirectory + @"\" + Sorter.SortCriteria.ToString();
            sortDirectory = Sorter.CreateDirectory(sortDirectory);

            foreach (var durationInterval in ReplayDurations)
            {
                string DurationName = null;
                if (durationInterval.Key != -1)
                {
                    string previousDuration = null;
                    int DurationIndex = GetFirstIndex(SortCriteriaParameters.Durations, durationInterval.Key);
                    if (DurationIndex != 0)
                    {
                        previousDuration = SortCriteriaParameters.Durations[DurationIndex - 1].ToString() + "m";
                    }
                    else
                    {
                        previousDuration = "0m";
                    }
                    DurationName = previousDuration + "-" + durationInterval.Key.ToString() + "m";
                }
                else
                {
                    DurationName = SortCriteriaParameters.Durations[SortCriteriaParameters.Durations.Length - 1].ToString() + "m++";
                }
                try
                {
                    Directory.CreateDirectory(sortDirectory + @"\" + DurationName);
                    var DurationReplays = ReplayDurations[durationInterval.Key];
                    IDictionary<string, IReplay> FileReplays = new Dictionary<string, IReplay>();
                    DirectoryFileReplay.Add(new KeyValuePair<string, IDictionary<string, IReplay>>(sortDirectory + @"\" + DurationName, FileReplays));
                    foreach (var replay in DurationReplays)
                    {
                        try
                        {
                            string File = string.Empty;
                            if (IsNested == false)
                            {
                                File = ReplayHandler.CopyReplay(Sorter.ListReplays, replay, Sorter.Files, sortDirectory, DurationName, KeepOriginalReplayNames, Sorter.CustomReplayFormat);
                            }
                            else
                            {
                                File = ReplayHandler.MoveReplay(Sorter.ListReplays, replay, Sorter.Files, sortDirectory, DurationName, KeepOriginalReplayNames, Sorter.CustomReplayFormat);
                            }
                            
                            FileReplays.Add(new KeyValuePair<string, IReplay>(/*Sorter.Files.ElementAt(Sorter.ListReplays.IndexOf(replay))*/File, replay));
                        }
                        catch (IOException IOex)
                        {
                            ErrorLogger.LogError("SortOnDuration IOException.", Sorter.OriginalDirectory + @"\LogErrors", IOex);
                            //Console.WriteLine(IOex.Message);
                        }
                        catch (NotSupportedException NSE)
                        {
                            ErrorLogger.LogError("SortOnDuration NotSupportedException.", Sorter.OriginalDirectory + @"\LogErrors", NSE);
                            //Console.WriteLine(NSE.Message);
                        }
                        catch (NullReferenceException nullex)
                        {
                            ErrorLogger.LogError("SortOnDuration NullReferenceException.", Sorter.OriginalDirectory + @"\LogErrors", nullex);
                            //Console.WriteLine(nullex.Message);
                        }
                        catch (ArgumentException AEX)
                        {
                            ErrorLogger.LogError("SortOnDuration ArgumentException.", Sorter.OriginalDirectory + @"\LogErrors", AEX);
                            //Console.WriteLine(AEX.Message);
                        }
                        catch (Exception ex)
                        {
                            ErrorLogger.LogError("SortOnDuration Exception.", Sorter.OriginalDirectory + @"\LogErrors", ex);
                            //Console.WriteLine(ex.Message);
                        }
                    }
                }
                catch (Exception ex)
                {
                    ErrorLogger.LogError("SortOnDuration Exception Outer.", Sorter.OriginalDirectory + @"\LogErrors", ex);
                    //Console.WriteLine(ex.Message);
                }
            }
            // not implemented yet
            return DirectoryFileReplay;
        }

        private int GetFirstIndex(int[] array, int number)
        {
            for (int i = 0; i < array.Length; i++)
            {
                if (array[i] == number)
                {
                    return i;
                }
            }
            return -1;
        }

        public IDictionary<string, IDictionary<string, IReplay>> SortAsync(BackgroundWorker worker_ReplaySorter, int currentCriteria, int numberOfCriteria, int currentPositionNested, int numberOfPositions)
        {
            if (SortCriteriaParameters.Durations == null)
            {
                throw new ArgumentException("Duration intervals cannot be null");
            }
            // Dictionary<directory, dictionary<file, replay>>
            IDictionary<string, IDictionary<string, IReplay>> DirectoryFileReplay = new Dictionary<string, IDictionary<string, IReplay>>();

            IDictionary<int, List<IReplay>> ReplayDurations = new Dictionary<int, List<IReplay>>();

            foreach (var replay in Sorter.ListReplays)
            {
                TimeSpan replayDuration = TimeSpan.FromSeconds((replay.FrameCount / ((double)1000 / 42)));
                double replayDurationInMinutes = replayDuration.TotalMinutes;
                int durationInterval = 0;
                while (replayDurationInMinutes > SortCriteriaParameters.Durations[durationInterval])
                {
                    durationInterval++;
                    if (durationInterval == SortCriteriaParameters.Durations.Length)
                    {
                        break;
                    }
                }

                if (durationInterval != SortCriteriaParameters.Durations.Length)
                {
                    if (!ReplayDurations.ContainsKey(SortCriteriaParameters.Durations[durationInterval]))
                    {
                        ReplayDurations.Add(new KeyValuePair<int, List<IReplay>>(SortCriteriaParameters.Durations[durationInterval], new List<IReplay> { replay }));
                    }
                    else
                    {
                        ReplayDurations[SortCriteriaParameters.Durations[durationInterval]].Add(replay);
                    }
                    // => throws error key does not exist !!! => ReplayDurations[durations[durationInterval]].Add(replay);
                }
                else
                {
                    if (!ReplayDurations.ContainsKey(-1))
                    {
                        ReplayDurations.Add(new KeyValuePair<int, List<IReplay>>(-1, new List<IReplay> { replay }));
                    }
                    else
                    {
                        ReplayDurations[-1].Add(replay);
                    }

                }
            }

            string sortDirectory = Sorter.CurrentDirectory + @"\" + Sorter.SortCriteria.ToString();
            sortDirectory = Sorter.CreateDirectory(sortDirectory, true);
            int currentPosition = 0;
            int progressPercentage = 0;

            foreach (var durationInterval in ReplayDurations)
            {
                string DurationName = null;
                if (durationInterval.Key != -1)
                {
                    string previousDuration = null;
                    int DurationIndex = GetFirstIndex(SortCriteriaParameters.Durations, durationInterval.Key);
                    if (DurationIndex != 0)
                    {
                        previousDuration = SortCriteriaParameters.Durations[DurationIndex - 1].ToString() + "m";
                    }
                    else
                    {
                        previousDuration = "0m";
                    }
                    DurationName = previousDuration + "-" + durationInterval.Key.ToString() + "m";
                }
                else
                {
                    DurationName = SortCriteriaParameters.Durations[SortCriteriaParameters.Durations.Length - 1].ToString() + "m++";
                }
                try
                {
                    Directory.CreateDirectory(sortDirectory + @"\" + DurationName);
                    var DurationReplays = ReplayDurations[durationInterval.Key];
                    IDictionary<string, IReplay> FileReplays = new Dictionary<string, IReplay>();
                    DirectoryFileReplay.Add(new KeyValuePair<string, IDictionary<string, IReplay>>(sortDirectory + @"\" + DurationName, FileReplays));
                    foreach (var replay in DurationReplays)
                    {
                        if (worker_ReplaySorter.CancellationPending == true)
                        {
                            // ??? how am i supposed to do this!! This doesn't feel right at all!! No way i'm supposed to also pass the DoWorkEventArgs!!
                            return null;
                        }
                        try
                        {
                            string File = string.Empty;
                            if (IsNested == false)
                            {
                                File = ReplayHandler.CopyReplay(Sorter.ListReplays, replay, Sorter.Files, sortDirectory, DurationName, KeepOriginalReplayNames, Sorter.CustomReplayFormat);
                            }
                            else
                            {
                                File = ReplayHandler.MoveReplay(Sorter.ListReplays, replay, Sorter.Files, sortDirectory, DurationName, KeepOriginalReplayNames, Sorter.CustomReplayFormat);
                            }

                            FileReplays.Add(new KeyValuePair<string, IReplay>(/*Sorter.Files.ElementAt(Sorter.ListReplays.IndexOf(replay))*/File, replay));
                        }
                        catch (IOException IOex)
                        {
                            ErrorLogger.LogError("SortOnDuration IOException.", Sorter.OriginalDirectory + @"\LogErrors", IOex);
                            //Console.WriteLine(IOex.Message);
                        }
                        catch (NotSupportedException NSE)
                        {
                            ErrorLogger.LogError("SortOnDuration NotSupportedException.", Sorter.OriginalDirectory + @"\LogErrors", NSE);
                            //Console.WriteLine(NSE.Message);
                        }
                        catch (NullReferenceException nullex)
                        {
                            ErrorLogger.LogError("SortOnDuration NullReferenceException.", Sorter.OriginalDirectory + @"\LogErrors", nullex);
                            //Console.WriteLine(nullex.Message);
                        }
                        catch (ArgumentException AEX)
                        {
                            ErrorLogger.LogError("SortOnDuration ArgumentException.", Sorter.OriginalDirectory + @"\LogErrors", AEX);
                            //Console.WriteLine(AEX.Message);
                        }
                        catch (Exception ex)
                        {
                            ErrorLogger.LogError("SortOnDuration Exception.", Sorter.OriginalDirectory + @"\LogErrors", ex);
                            //Console.WriteLine(ex.Message);
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
                        worker_ReplaySorter.ReportProgress(progressPercentage, "Sorting on duration...");
                    }
                }
                catch (Exception ex)
                {
                    ErrorLogger.LogError("SortOnDuration Exception Outer.", Sorter.OriginalDirectory + @"\LogErrors", ex);
                    //Console.WriteLine(ex.Message);
                }
            }
            // not implemented yet
            return DirectoryFileReplay;
        }
    }
}
