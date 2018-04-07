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

        public IDictionary<string, IDictionary<string, IReplay>> Sort()
        {
            // Dictionary<directory, dictionary<file, replay>>
            IDictionary<string, IDictionary<string, IReplay>> DirectoryFileReplay = new Dictionary<string, IDictionary<string, IReplay>>();

            // replays grouped by gametype
            var ReplaysByGameTypes = from replay in Sorter.ListReplays
                                     group replay by replay.GameType;

            // make sortdirectory
            string sortDirectory = Sorter.CurrentDirectory + @"\" + Sorter.SortCriteria.ToString();
            sortDirectory = Sorter.CreateDirectory(sortDirectory);

            // make subdirectory per gametype, and put all associated replays into it

            foreach (var gametype in ReplaysByGameTypes)
            {
                var GameType = gametype.Key.ToString();
                Directory.CreateDirectory(sortDirectory + @"\" + GameType);
                IDictionary<string, IReplay> FileReplays = new Dictionary<string, IReplay>();
                DirectoryFileReplay.Add(new KeyValuePair<string, IDictionary<string, IReplay>>(sortDirectory + @"\" + GameType, FileReplays));

                foreach (var replay in gametype)
                {
                    try
                    {
                        string File = string.Empty;
                        if (IsNested == false)
                        {
                            File = ReplayHandler.CopyReplay(Sorter.ListReplays, replay, Sorter.Files, sortDirectory, GameType, KeepOriginalReplayNames, Sorter.CustomReplayFormat);
                        }
                        else
                        {
                            File = ReplayHandler.MoveReplay(Sorter.ListReplays, replay, Sorter.Files, sortDirectory, GameType, KeepOriginalReplayNames, Sorter.CustomReplayFormat);
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
                        //Console.WriteLine(NSE.Message);
                    }
                    catch (NullReferenceException nullex)
                    {
                        ErrorLogger.LogError("SortOnGameType NullReferenceException.", Sorter.OriginalDirectory + @"\LogErrors", nullex);
                        //Console.WriteLine(nullex.Message);
                    }
                    catch (ArgumentException AEX)
                    {
                        ErrorLogger.LogError("SortOnGameType ArgumentException.", Sorter.OriginalDirectory + @"\LogErrors", AEX);
                        //Console.WriteLine(AEX.Message);
                    }
                }
            }
            // not implemented yet
            return DirectoryFileReplay;
        }

        public IDictionary<string, IDictionary<string, IReplay>> SortAsync(BackgroundWorker worker_ReplaySorter, int currentCriteria, int numberOfCriteria, int currentPositionNested, int numberOfPositions)
        {
            // Dictionary<directory, dictionary<file, replay>>
            IDictionary<string, IDictionary<string, IReplay>> DirectoryFileReplay = new Dictionary<string, IDictionary<string, IReplay>>();

            // replays grouped by gametype
            var ReplaysByGameTypes = from replay in Sorter.ListReplays
                                     group replay by replay.GameType;

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
                IDictionary<string, IReplay> FileReplays = new Dictionary<string, IReplay>();
                DirectoryFileReplay.Add(new KeyValuePair<string, IDictionary<string, IReplay>>(sortDirectory + @"\" + GameType, FileReplays));

                foreach (var replay in gametype)
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
                    worker_ReplaySorter.ReportProgress(progressPercentage, "sorting on gametype...");
                    try
                    {
                        string File = string.Empty;
                        if (IsNested == false)
                        {
                            File = ReplayHandler.CopyReplay(Sorter.ListReplays, replay, Sorter.Files, sortDirectory, GameType, KeepOriginalReplayNames, Sorter.CustomReplayFormat);
                        }
                        else
                        {
                            File = ReplayHandler.MoveReplay(Sorter.ListReplays, replay, Sorter.Files, sortDirectory, GameType, KeepOriginalReplayNames, Sorter.CustomReplayFormat);
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
                        //Console.WriteLine(NSE.Message);
                    }
                    catch (NullReferenceException nullex)
                    {
                        ErrorLogger.LogError("SortOnGameType NullReferenceException.", Sorter.OriginalDirectory + @"\LogErrors", nullex);
                        //Console.WriteLine(nullex.Message);
                    }
                    catch (ArgumentException AEX)
                    {
                        ErrorLogger.LogError("SortOnGameType ArgumentException.", Sorter.OriginalDirectory + @"\LogErrors", AEX);
                        //Console.WriteLine(AEX.Message);
                    }
                }
            }
            // not implemented yet
            return DirectoryFileReplay;
        }
    }
}
