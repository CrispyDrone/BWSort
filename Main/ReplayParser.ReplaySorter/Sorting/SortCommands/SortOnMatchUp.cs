using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ReplayParser.ReplaySorter.ReplayRenamer;
using System.IO;
using ReplayParser.Interfaces;

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
                        MatchUp MatchUp = new MatchUp(replay);
                        // int => team
                        IDictionary<int, IDictionary<RaceType, int>> EncodedMatchUp = new Dictionary<int, IDictionary<RaceType, int>>();
                        int team = 1;
                        foreach (var RaceCombination in MatchUp.TeamRaces)
                        {
                            var RaceFrequency = Sorter.EncodeRacesFrequency(RaceCombination);
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
                    Console.WriteLine(nullex.Message);
                }

            }

            string sortDirectory = Sorter.CurrentDirectory + @"\" + Sorter.SortCriteria.ToString();
            Sorter.CreateDirectory(sortDirectory);

            
            foreach (var matchup in /*MatchUps.Distinct(MatchUpEq)*/ReplayMatchUp.Keys)
            {
                // make directory per matchup
                var MatchUpName = Sorter.MatchUpToString(matchup);
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
                        Console.WriteLine(IOex.Message);
                    }
                    catch (NotSupportedException NSE)
                    {
                        Console.WriteLine(NSE.Message);
                    }
                    catch (NullReferenceException nullex)
                    {
                        Console.WriteLine(nullex.Message);
                    }
                    catch (ArgumentException AEX)
                    {
                        Console.WriteLine(AEX.Message);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }
            }
            // not implemented yet
            return DirectoryFileReplay;
        }
    }
}
