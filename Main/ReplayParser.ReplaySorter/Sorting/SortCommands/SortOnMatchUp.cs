﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ReplayParser.ReplaySorter.ReplayRenamer;
using System.IO;
using ReplayParser.Interfaces;
using ReplayParser.ReplaySorter.Diagnostics;
using System.ComponentModel;

namespace ReplayParser.ReplaySorter.Sorting.SortCommands
{
    public class SortOnMatchUp : ISortCommand
    {
        public SortOnMatchUp(SortCriteriaParameters sortcriteriaparameters, bool keeporiginalreplaynames, Sorter sorter)
        {
            SortCriteriaParameters = sortcriteriaparameters;
            KeepOriginalReplayNames = keeporiginalreplaynames;
            Sorter = sorter;
        }
        public bool KeepOriginalReplayNames { get; set; }
        public SortCriteriaParameters SortCriteriaParameters { get; set; }
        public Criteria SortCriteria { get { return Criteria.MATCHUP; } }
        public bool IsNested { get; set; }
        public Sorter Sorter { get; set; }
        public IDictionary<string, IDictionary<string, IReplay>> Sort()
        {
            // Dictionary<directory, dictionary<file, replay>>
            IDictionary<string, IDictionary<string, IReplay>> DirectoryFileReplay = new Dictionary<string, IDictionary<string, IReplay>>();

            // get all matchups from the replays
            // allow the ignoring of specific game types

            // you could pass the IEquality comparer to the constructor of the dictionary
            MatchUpEqualityComparer MatchUpEq = new MatchUpEqualityComparer();
            //IDictionary<int, IDictionary<RaceType, int>> MatchUps = new Dictionary<int, IDictionary<RaceType, int>>();
            IDictionary<IDictionary<int, IDictionary<RaceType, int>>, IList<Interfaces.IReplay>> ReplayMatchUp = new Dictionary<IDictionary<int, IDictionary<RaceType, int>>, IList<Interfaces.IReplay>>(MatchUpEq);

            foreach (var replay in Sorter.ListReplays)
            {
                try
                {
                    if (SortCriteriaParameters.ValidGameTypes[replay.GameType] == true)
                    {
                        Team Team = new Team(replay);
                        MatchUp MatchUp = new MatchUp(replay, Team);
                        // int => team
                        IDictionary<int, IDictionary<RaceType, int>> EncodedMatchUp = new Dictionary<int, IDictionary<RaceType, int>>();
                        int team = 1;
                        foreach (var RaceCombination in MatchUp.TeamRaces)
                        {
                            var RaceFrequency = EncodeRacesFrequency(RaceCombination);
                            EncodedMatchUp.Add(new KeyValuePair<int, IDictionary<RaceType, int>>(team, RaceFrequency));
                            team++;
                        }
                        //MatchUps.Add(EncodedMatchUp);
                        if (!ReplayMatchUp.ContainsKey(EncodedMatchUp))
                        {
                            ReplayMatchUp.Add(new KeyValuePair<IDictionary<int, IDictionary<RaceType, int>>, IList<Interfaces.IReplay>>(EncodedMatchUp, new List<Interfaces.IReplay> { replay }));
                        }
                        else
                        {
                            ReplayMatchUp[EncodedMatchUp].Add(replay);
                        }
                    }
                }
                catch (NullReferenceException nullex)
                {
                    ErrorLogger.LogError($"SortOnMatchUp NullReferenceException: Encoding matchups, file: {Sorter.Files.ElementAt(Sorter.ListReplays.IndexOf(replay))}", Sorter.OriginalDirectory + @"\LogErrors", nullex);
                    //Console.WriteLine(nullex.Message);
                }

            }

            string sortDirectory = Sorter.CurrentDirectory + @"\" + Sorter.SortCriteria.ToString();
            sortDirectory = Sorter.CreateDirectory(sortDirectory);

            
            foreach (var matchup in /*MatchUps.Distinct(MatchUpEq)*/ReplayMatchUp.Keys)
            {
                // make directory per matchup
                var MatchUpName = MatchUpToString(matchup);
                Directory.CreateDirectory(sortDirectory + @"\" + MatchUpName);

                IDictionary<string, IReplay> FileReplays = new Dictionary<string, IReplay>();
                DirectoryFileReplay.Add(new KeyValuePair<string, IDictionary<string, IReplay>>(sortDirectory + @"\" + MatchUpName, FileReplays));

                // write all associated replays to this directory
                var MatchUpReplays = ReplayMatchUp[matchup];
                foreach (var replay in MatchUpReplays)
                {
                    try
                    {
                        string File = string.Empty;
                        if (IsNested == false)
                        {
                            File = ReplayHandler.CopyReplay(Sorter.ListReplays, replay, Sorter.Files, sortDirectory, MatchUpName, KeepOriginalReplayNames, Sorter.CustomReplayFormat);
                        }
                        else
                        {
                            File = ReplayHandler.MoveReplay(Sorter.ListReplays, replay, Sorter.Files, sortDirectory, MatchUpName, KeepOriginalReplayNames, Sorter.CustomReplayFormat);
                        }
                        
                        FileReplays.Add(new KeyValuePair<string, IReplay>(/*Sorter.Files.ElementAt(Sorter.ListReplays.IndexOf(replay))*/File, replay));
                    }
                    catch (IOException IOex)
                    {
                        ErrorLogger.LogError($"SortOnMatchUp IOException", Sorter.OriginalDirectory + @"\LogErrors", IOex);
                        //Console.WriteLine(IOex.Message);
                    }
                    catch (NotSupportedException NSE)
                    {
                        ErrorLogger.LogError($"SortOnMatchUp NotSupportedException", Sorter.OriginalDirectory + @"\LogErrors", NSE);
                        //Console.WriteLine(NSE.Message);
                    }
                    catch (NullReferenceException nullex)
                    {
                        ErrorLogger.LogError($"SortOnMatchUp NullReferenceException", Sorter.OriginalDirectory + @"\LogErrors", nullex);
                        //Console.WriteLine(nullex.Message);
                    }
                    catch (ArgumentException AEX)
                    {
                        ErrorLogger.LogError($"SortOnMatchUp ArgumentException", Sorter.OriginalDirectory + @"\LogErrors", AEX);
                        //Console.WriteLine(AEX.Message);
                    }
                    catch (Exception ex)
                    {
                        ErrorLogger.LogError($"SortOnMatchUp Exception", Sorter.OriginalDirectory + @"\LogErrors", ex);
                        //Console.WriteLine(ex.Message);
                    }
                }
            }
            // not implemented yet
            return DirectoryFileReplay;
        }
        private IDictionary<RaceType, int> EncodeRacesFrequency(string raceCombination)
        {
            IDictionary<RaceType, int> EncodedRacesFrequency = new Dictionary<RaceType, int>();

            foreach (var Race in Enum.GetNames(typeof(RaceType)))
            {
                int RaceFrequency = raceCombination.Select((r, i) => r == Race.First() ? i : -1).Where(i => i != -1).Count();
                EncodedRacesFrequency.Add(new KeyValuePair<RaceType, int>((RaceType)Enum.Parse(typeof(RaceType), Race), RaceFrequency));
            }
            return EncodedRacesFrequency;
        }

        private string MatchUpToString(IDictionary<int, IDictionary<RaceType, int>> MatchUpValues)
        {
            StringBuilder MatchUpString = new StringBuilder();
            foreach (var team in MatchUpValues)
            {
                foreach (var Race in team.Value)
                {
                    MatchUpString.Append(Race.Key.ToString().First(), Race.Value);
                }
                MatchUpString.Append("vs");
            }
            MatchUpString.Remove(MatchUpString.Length - 2, 2);
            return MatchUpString.ToString();
        }

        public IDictionary<string, IDictionary<string, IReplay>> SortAsync(BackgroundWorker worker_ReplaySorter, int currentCriteria, int numberOfCriteria, int currentPositionNested, int numberOfPositions)
        {
            // Dictionary<directory, dictionary<file, replay>>
            IDictionary<string, IDictionary<string, IReplay>> DirectoryFileReplay = new Dictionary<string, IDictionary<string, IReplay>>();

            // get all matchups from the replays
            // allow the ignoring of specific game types

            // you could pass the IEquality comparer to the constructor of the dictionary
            MatchUpEqualityComparer MatchUpEq = new MatchUpEqualityComparer();
            //IDictionary<int, IDictionary<RaceType, int>> MatchUps = new Dictionary<int, IDictionary<RaceType, int>>();
            IDictionary<IDictionary<int, IDictionary<RaceType, int>>, IList<Interfaces.IReplay>> ReplayMatchUp = new Dictionary<IDictionary<int, IDictionary<RaceType, int>>, IList<Interfaces.IReplay>>(MatchUpEq);

            foreach (var replay in Sorter.ListReplays)
            {
                try
                {
                    if (SortCriteriaParameters.ValidGameTypes[replay.GameType] == true)
                    {
                        Team Team = new Team(replay);
                        MatchUp MatchUp = new MatchUp(replay, Team);
                        // int => team
                        IDictionary<int, IDictionary<RaceType, int>> EncodedMatchUp = new Dictionary<int, IDictionary<RaceType, int>>();
                        int team = 1;
                        foreach (var RaceCombination in MatchUp.TeamRaces)
                        {
                            var RaceFrequency = EncodeRacesFrequency(RaceCombination);
                            EncodedMatchUp.Add(new KeyValuePair<int, IDictionary<RaceType, int>>(team, RaceFrequency));
                            team++;
                        }
                        if (!ReplayMatchUp.ContainsKey(EncodedMatchUp))
                        {
                            ReplayMatchUp.Add(new KeyValuePair<IDictionary<int, IDictionary<RaceType, int>>, IList<Interfaces.IReplay>>(EncodedMatchUp, new List<Interfaces.IReplay> { replay }));
                        }
                        else
                        {
                            ReplayMatchUp[EncodedMatchUp].Add(replay);
                        }
                    }
                }
                catch (NullReferenceException nullex)
                {
                    ErrorLogger.LogError($"SortOnMatchUp NullReferenceException: Encoding matchups, file: {Sorter.Files.ElementAt(Sorter.ListReplays.IndexOf(replay))}", Sorter.OriginalDirectory + @"\LogErrors", nullex);
                }

            }

            string sortDirectory = Sorter.CurrentDirectory + @"\" + Sorter.SortCriteria.ToString();
            sortDirectory = Sorter.CreateDirectory(sortDirectory, true);
            int currentPosition = 0;
            int progressPercentage = 0;

            foreach (var matchup in ReplayMatchUp.Keys)
            {
                // make directory per matchup
                var MatchUpName = MatchUpToString(matchup);
                Directory.CreateDirectory(sortDirectory + @"\" + MatchUpName);

                IDictionary<string, IReplay> FileReplays = new Dictionary<string, IReplay>();
                DirectoryFileReplay.Add(new KeyValuePair<string, IDictionary<string, IReplay>>(sortDirectory + @"\" + MatchUpName, FileReplays));

                // write all associated replays to this directory
                var MatchUpReplays = ReplayMatchUp[matchup];
                foreach (var replay in MatchUpReplays)
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
                    worker_ReplaySorter.ReportProgress(progressPercentage, "sorting on matchup...");
                    try
                    {
                        string File = string.Empty;
                        if (IsNested == false)
                        {
                            File = ReplayHandler.CopyReplay(Sorter.ListReplays, replay, Sorter.Files, sortDirectory, MatchUpName, KeepOriginalReplayNames, Sorter.CustomReplayFormat);
                        }
                        else
                        {
                            File = ReplayHandler.MoveReplay(Sorter.ListReplays, replay, Sorter.Files, sortDirectory, MatchUpName, KeepOriginalReplayNames, Sorter.CustomReplayFormat);
                        }

                        FileReplays.Add(new KeyValuePair<string, IReplay>(/*Sorter.Files.ElementAt(Sorter.ListReplays.IndexOf(replay))*/File, replay));
                    }
                    catch (IOException IOex)
                    {
                        ErrorLogger.LogError($"SortOnMatchUp IOException", Sorter.OriginalDirectory + @"\LogErrors", IOex);
                    }
                    catch (NotSupportedException NSE)
                    {
                        ErrorLogger.LogError($"SortOnMatchUp NotSupportedException", Sorter.OriginalDirectory + @"\LogErrors", NSE);
                    }
                    catch (NullReferenceException nullex)
                    {
                        ErrorLogger.LogError($"SortOnMatchUp NullReferenceException", Sorter.OriginalDirectory + @"\LogErrors", nullex);
                    }
                    catch (ArgumentException AEX)
                    {
                        ErrorLogger.LogError($"SortOnMatchUp ArgumentException", Sorter.OriginalDirectory + @"\LogErrors", AEX);
                    }
                    catch (Exception ex)
                    {
                        ErrorLogger.LogError($"SortOnMatchUp Exception", Sorter.OriginalDirectory + @"\LogErrors", ex);
                    }
                }
            }
            // not implemented yet
            return DirectoryFileReplay;
        }
    }
}
