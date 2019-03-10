using System;
using System.Collections.Generic;
using System.IO;
using ReplayParser.Interfaces;
using ReplayParser.ReplaySorter.Diagnostics;
using System.ComponentModel;
using ReplayParser.ReplaySorter.IO;

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

        public IDictionary<string, List<File<IReplay>>> Sort(List<string> replaysThrowingExceptions)
        {
            if (SortCriteriaParameters.Durations == null)
            {
                throw new ArgumentException("Duration intervals cannot be null");
            }
            // Dictionary<directory, dictionary<file, replay>>
            IDictionary<string, List<File<IReplay>>> DirectoryFileReplay = new Dictionary<string, List<File<IReplay>>>();

            IDictionary<int, List<File<IReplay>>> ReplayDurations = new Dictionary<int, List<File<IReplay>>>();

            foreach (var replay in Sorter.ListReplays)
            {
                TimeSpan replayDuration = TimeSpan.FromSeconds((replay.Content.FrameCount / ((double)1000 / 42)));
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
                        ReplayDurations.Add(new KeyValuePair<int, List<File<IReplay>>>(SortCriteriaParameters.Durations[durationInterval], new List<File<IReplay>> { replay }));
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
                        ReplayDurations.Add(new KeyValuePair<int, List<File<IReplay>>>(-1, new List<File<IReplay>> { replay }));
                    }
                    else
                    {
                        ReplayDurations[-1].Add(replay);
                    }

                }
            }

            string sortDirectory = Sorter.CurrentDirectory + @"\" + Sorter.SortCriteria.ToString();
            sortDirectory = FileHandler.CreateDirectory(sortDirectory);

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
                    var FileReplays = new List<File<IReplay>>();
                    DirectoryFileReplay.Add(new KeyValuePair<string, List<File<IReplay>>>(sortDirectory + @"\" + DurationName, FileReplays));
                    foreach (var replay in DurationReplays)
                    {
                        bool threwException = false;
                        try
                        {
                            if (IsNested == false)
                            {
                                ReplayHandler.CopyReplay(replay, sortDirectory, DurationName, KeepOriginalReplayNames, Sorter.CustomReplayFormat);
                            }
                            else
                            {
                                ReplayHandler.MoveReplay(replay, sortDirectory, DurationName, KeepOriginalReplayNames, Sorter.CustomReplayFormat);
                            }
                            
                            FileReplays.Add(replay);
                        }
                        catch (IOException IOex)
                        {
                            threwException = true;
                            ErrorLogger.GetInstance()?.LogError("SortOnDuration IOException.", ex: IOex);
                        }
                        catch (NotSupportedException NSE)
                        {
                            threwException = true;
                            ErrorLogger.GetInstance()?.LogError("SortOnDuration NotSupportedException.", ex: NSE);
                        }
                        catch (NullReferenceException nullex)
                        {
                            threwException = true;
                            ErrorLogger.GetInstance()?.LogError("SortOnDuration NullReferenceException.", ex: nullex);
                        }
                        catch (ArgumentException AEX)
                        {
                            threwException = true;
                            ErrorLogger.GetInstance()?.LogError("SortOnDuration ArgumentException.", ex: AEX);
                        }
                        catch (Exception ex)
                        {
                            threwException = true;
                            ErrorLogger.GetInstance()?.LogError("SortOnDuration Exception.", ex: ex);
                        }
                        if (threwException)
                            replaysThrowingExceptions.Add(replay.OriginalFilePath);
                    }
                }
                catch (Exception ex)
                {
                    ErrorLogger.GetInstance()?.LogError("SortOnDuration Exception Outer.", ex: ex);
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

        public IDictionary<string, List<File<IReplay>>> SortAsync(List<string> replaysThrowingExceptions, BackgroundWorker worker_ReplaySorter, int currentCriteria, int numberOfCriteria, int currentPositionNested, int numberOfPositions)
        {
            if (SortCriteriaParameters.Durations == null)
            {
                throw new ArgumentException("Duration intervals cannot be null");
            }
            // Dictionary<directory, dictionary<file, replay>>
            IDictionary<string, List<File<IReplay>>> DirectoryFileReplay = new Dictionary<string, List<File<IReplay>>>();

            IDictionary<int, List<File<IReplay>>> ReplayDurations = new Dictionary<int, List<File<IReplay>>>();

            foreach (var replay in Sorter.ListReplays)
            {
                TimeSpan replayDuration = TimeSpan.FromSeconds((replay.Content.FrameCount / ((double)1000 / 42)));
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
                        ReplayDurations.Add(new KeyValuePair<int, List<File<IReplay>>>(SortCriteriaParameters.Durations[durationInterval], new List<File<IReplay>> { replay }));
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
                        ReplayDurations.Add(new KeyValuePair<int, List<File<IReplay>>>(-1, new List<File<IReplay>> { replay }));
                    }
                    else
                    {
                        ReplayDurations[-1].Add(replay);
                    }

                }
            }

            string sortDirectory = Sorter.CurrentDirectory + @"\" + Sorter.SortCriteria.ToString();
            sortDirectory = FileHandler.CreateDirectory(sortDirectory, true);
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
                    var FileReplays = new List<File<IReplay>>();
                    DirectoryFileReplay.Add(new KeyValuePair<string, List<File<IReplay>>>(sortDirectory + @"\" + DurationName, FileReplays));
                    foreach (var replay in DurationReplays)
                    {
                        bool threwError = false;
                        if (worker_ReplaySorter.CancellationPending == true)
                        {
                            // ??? how am i supposed to do this!! This doesn't feel right at all!! No way i'm supposed to also pass the DoWorkEventArgs!!
                            return null;
                        }
                        try
                        {
                            if (IsNested == false)
                            {
                                ReplayHandler.CopyReplay(replay, sortDirectory, DurationName, KeepOriginalReplayNames, Sorter.CustomReplayFormat);
                            }
                            else
                            {
                                ReplayHandler.MoveReplay(replay, sortDirectory, DurationName, KeepOriginalReplayNames, Sorter.CustomReplayFormat);
                            }

                            FileReplays.Add(replay);
                        }
                        catch (IOException IOex)
                        {
                            threwError = true;
                            ErrorLogger.GetInstance()?.LogError("SortOnDuration IOException.", ex: IOex);
                        }
                        catch (NotSupportedException NSE)
                        {
                            threwError = true;
                            ErrorLogger.GetInstance()?.LogError("SortOnDuration NotSupportedException.", ex: NSE);
                        }
                        catch (NullReferenceException nullex)
                        {
                            threwError = true;
                            ErrorLogger.GetInstance()?.LogError("SortOnDuration NullReferenceException.", ex: nullex);
                        }
                        catch (ArgumentException AEX)
                        {
                            threwError = true;
                            ErrorLogger.GetInstance()?.LogError("SortOnDuration ArgumentException.", ex: AEX);
                        }
                        catch (Exception ex)
                        {
                            threwError = true;
                            ErrorLogger.GetInstance()?.LogError("SortOnDuration Exception.", ex: ex);
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
                        if (threwError)
                            replaysThrowingExceptions.Add(replay.OriginalFilePath);
                        worker_ReplaySorter.ReportProgress(progressPercentage, "Sorting on duration...");
                    }
                }
                catch (Exception ex)
                {
                    ErrorLogger.GetInstance()?.LogError("SortOnDuration Exception Outer.", ex: ex);
                }
            }
            // not implemented yet
            return DirectoryFileReplay;
        }
    }
}
