using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using ReplayParser.Interfaces;
using ReplayParser.ReplaySorter.Diagnostics;
using System.ComponentModel;

namespace ReplayParser.ReplaySorter.Sorting.SortCommands
{
    public class SortOnGameType : ISortCommand
    {
        public SortOnGameType(SortCriteriaParameters sortcriteriaparameters, bool keeporiginalreplaynames, Sorter sorter)
        {
            SortCriteriaParameters = sortcriteriaparameters;
            KeepOriginalReplayNames = keeporiginalreplaynames;
            Sorter = sorter;
        }
        public bool KeepOriginalReplayNames { get; set; }

        public SortCriteriaParameters SortCriteriaParameters { get; set; }
        public Criteria SortCriteria { get { return Criteria.GAMETYPE; } }
        public bool IsNested { get; set; }
        public Sorter Sorter { get; set; }

        public IDictionary<string, List<File<IReplay>>> Sort(List<string> replaysThrowingExceptions)
        {
            // Dictionary<directory, dictionary<file, replay>>
            IDictionary<string, List<File<IReplay>>> DirectoryFileReplay = new Dictionary<string, List<File<IReplay>>>();

            // replays grouped by gametype
            var ReplaysByGameTypes = from replay in Sorter.ListReplays
                                     group replay by replay.Content.GameType;

            // make sortdirectory
            string sortDirectory = Sorter.CurrentDirectory + @"\" + Sorter.SortCriteria.ToString();
            sortDirectory = Sorter.CreateDirectory(sortDirectory);

            // make subdirectory per gametype, and put all associated replays into it

            foreach (var gametype in ReplaysByGameTypes)
            {
                var GameType = gametype.Key.ToString();
                Directory.CreateDirectory(sortDirectory + @"\" + GameType);
                var FileReplays = new List<File<IReplay>>();
                DirectoryFileReplay.Add(new KeyValuePair<string, List<File<IReplay>>>(sortDirectory + @"\" + GameType, FileReplays));

                foreach (var replay in gametype)
                {
                    bool threwException = false;
                    try
                    {
                        if (IsNested == false)
                        {
                            ReplayHandler.CopyReplay(replay, sortDirectory, GameType, KeepOriginalReplayNames, Sorter.CustomReplayFormat);
                        }
                        else
                        {
                            ReplayHandler.MoveReplay(replay, sortDirectory, GameType, KeepOriginalReplayNames, Sorter.CustomReplayFormat);
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
                    if (threwException)
                        replaysThrowingExceptions.Add(replay.OriginalFileName);
                }
            }
            // not implemented yet
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
            string sortDirectory = Sorter.CurrentDirectory + @"\" + Sorter.SortCriteria.ToString();
            sortDirectory = Sorter.CreateDirectory(sortDirectory, true);

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
                    bool threwError = false;
                    try
                    {
                        if (IsNested == false)
                        {
                            ReplayHandler.CopyReplay(replay, sortDirectory, GameType, KeepOriginalReplayNames, Sorter.CustomReplayFormat);
                        }
                        else
                        {
                            ReplayHandler.MoveReplay(replay, sortDirectory, GameType, KeepOriginalReplayNames, Sorter.CustomReplayFormat);
                        }

                        FileReplays.Add(replay);
                    }
                    catch (IOException IOex)
                    {
                        threwError = true;
                        ErrorLogger.LogError("SortOnGameType IOException.", Sorter.OriginalDirectory + @"\LogErrors", IOex);
                        //Console.WriteLine(IOex.Message);
                    }
                    catch (NotSupportedException NSE)
                    {
                        threwError = true;
                        ErrorLogger.LogError("SortOnGameType NotSupportedException.", Sorter.OriginalDirectory + @"\LogErrors", NSE);
                        //Console.WriteLine(NSE.Message);
                    }
                    catch (NullReferenceException nullex)
                    {
                        threwError = true;
                        ErrorLogger.LogError("SortOnGameType NullReferenceException.", Sorter.OriginalDirectory + @"\LogErrors", nullex);
                        //Console.WriteLine(nullex.Message);
                    }
                    catch (ArgumentException AEX)
                    {
                        threwError = true;
                        ErrorLogger.LogError("SortOnGameType ArgumentException.", Sorter.OriginalDirectory + @"\LogErrors", AEX);
                        //Console.WriteLine(AEX.Message);
                    }

                    if (threwError)
                        replaysThrowingExceptions.Add(replay.OriginalFileName);

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
                    worker_ReplaySorter.ReportProgress(progressPercentage, "sorting on gametype...");

                }
            }
            // not implemented yet
            return DirectoryFileReplay;
        }
    }
}
