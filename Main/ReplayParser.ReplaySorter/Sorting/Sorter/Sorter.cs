﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;
using ReplayParser.ReplaySorter.ReplayRenamer;
using ReplayParser.ReplaySorter.Sorting;
using ReplayParser.Interfaces;
using ReplayParser.ReplaySorter.UserInput;
using ReplayParser.ReplaySorter.Sorting.SortCommands;

namespace ReplayParser.ReplaySorter
{
    public class Sorter
    {
        public Sorter()
        {

        }

        public Sorter(string directory, IEnumerable<string> files, List<IReplay> listreplays, Criteria sortcriteria)
        {
            this.SortCriteria = sortcriteria;
            this.ListReplays = listreplays;
            this.CurrentDirectory = directory;
            this.Files = files;
        }

        public Criteria SortCriteria
        {
            get;
            set;
        }

        public string[] CriteriaStringOrder
        {
            get;
            set;
        }

        public List<IReplay> ListReplays
        {
            get;
            set;
        }

        public string CurrentDirectory
        {
            get;
            set;
        }

        public IEnumerable<string> Files
        {
            get;
            set;
        }

        public CustomReplayFormat CustomReplayFormat { get; set; }

        private SortCommandFactory Factory = new SortCommandFactory();

        public void ExecuteSort(SortCriteriaParameters sortcriteriaparameters, bool keeporiginalreplaynames)
        {
            // this is useless, you could have just written the if statements you do in passoncriteria here!!
            // you need to dynamically call functions at runtime somehow
            // because by doing this you will always execute the sort criteria in a SPECIFIC ORDER as defined in your ENUM instead of as given by your user!!

            //foreach (var aCriteria in Enum.GetNames(typeof(Criteria)))
            //{
            //    if (SortCriteria.HasFlag(((Criteria)Enum.Parse(typeof(Criteria), aCriteria))))
            //    {

            //        PassOnCriteria(((Criteria)Enum.Parse(typeof(Criteria), aCriteria)), sortcriteriaparamaters, keeporiginalreplaynames);
            //    }
            //}

            // why do i need this silly string array with the original order...
            IDictionary<string, IDictionary<string, IReplay>> SortOnXResult = new Dictionary<string, IDictionary<string, IReplay>>();
            for (int i = 0; i < CriteriaStringOrder.Length; i++)
            {
                var SortOnX = Factory.GetSortCommand((Criteria)Enum.Parse(typeof(Criteria), CriteriaStringOrder[i]), sortcriteriaparameters, keeporiginalreplaynames, this);
                if (i == 0)
                {
                    SortOnXResult = SortOnX.Sort();
                }
                else
                {
                    // nested sort
                    SortOnX.IsNested = true;
                    SortOnXResult = NestedSort(SortOnX, SortOnXResult);
                }

            }
        }

        public IDictionary<string, IDictionary<string, IReplay>> NestedSort(ISortCommand SortOnX, IDictionary<string, IDictionary<string, IReplay>> SortOnXResult)
        {
            // Dictionary<directory, dictionary<file, replay>>
            IDictionary<string, IDictionary<string, IReplay>> DirectoryFileReplay = new Dictionary<string, IDictionary<string, IReplay>>();

            // get replays
            // get files
            // set currentdirectory
            // on sorter
            // for replays and files, need to make return type for sort, which gives a dictionary for replays per directory

            SortOnX.Sorter.SortCriteria = SortOnX.SortCriteria;
            foreach (var directory in SortOnXResult.Keys)
            {
                var FileReplaysDictionary = SortOnXResult[directory];
                var Files = FileReplaysDictionary.Keys;
                var Replays = FileReplaysDictionary.Values.ToList();
                SortOnX.Sorter.CurrentDirectory = directory;
                SortOnX.Sorter.Files = Files;
                SortOnX.Sorter.ListReplays = Replays;
                var result = SortOnX.Sort();
                DirectoryFileReplay = DirectoryFileReplay.Concat(result).ToDictionary(k => k.Key, k => k.Value);
            }
            // not implemented yet
            return DirectoryFileReplay;
        }

        // I think you could make a command object for each SortOn function, and have one function that will return the proper object depending on which sortcriteria gets passed to it
        // then you execute the Sort() function on that command object, and it will execute?
        //public void PassOnCriteria(Criteria chosencriteria, SortCriteriaParameters sortcriteriaparamaters, bool keeporiginalreplaynames)
        //{
        //    if (chosencriteria.HasFlag(Criteria.PLAYERNAME))
        //    {
        //        SortOnPlayerName((bool)sortcriteriaparamaters.MakeFolderForWinner, (bool)sortcriteriaparamaters.MakeFolderForLoser, keeporiginalreplaynames);
        //    }

        //    if (chosencriteria.HasFlag(Criteria.GAMETYPE))
        //    {
        //        SortOnGameType(keeporiginalreplaynames);
        //    }

        //    if (chosencriteria.HasFlag(Criteria.MATCHUP))
        //    {
        //        SortOnMatchUp(keeporiginalreplaynames, sortcriteriaparamaters.ValidGameTypes);
        //    }

        //    if (chosencriteria.HasFlag(Criteria.MAP))
        //    {
        //        SortOnMap(keeporiginalreplaynames);
        //    }

        //    if (chosencriteria.HasFlag(Criteria.DURATION))
        //    {
        //        SortOnDuration(keeporiginalreplaynames, sortcriteriaparamaters.Durations);
        //    }

        //    //...
        //}

        //public void SortOnPlayerName(bool MakeFolderForWinner = true, bool MakeFolderForLoser = true, bool KeepOriginalReplayNames = true)
        //{
        //    //if (!MakeFolderForWinner && !MakeFolderForLoser)
        //    //{
        //    //    throw new ArgumentException("MakeFolderForWinner and MakeFolderForLoser cannot both be false at the same time.");
        //    //}

        //    List<string> PlayerNames = new List<string>();
        //    List<string> WinnersAndLosers = new List<string>();
        //    List<string> Winners = new List<string>();
        //    List<string> Losers = new List<string>();

        //    // Rewrite to PlayerNames.AddRange(ExtractPlayers(TypeOfPlayer, ListOfPlayer)....
        //    if (MakeFolderForWinner && MakeFolderForLoser)
        //    {
        //        // sort on playername
        //        PlayerNames.AddRange(ExtractPlayers(PlayerType.All, WinnersAndLosers));
        //    }
        //    else if (MakeFolderForWinner)
        //    {
        //        PlayerNames.AddRange(ExtractPlayers(PlayerType.Winner, Winners));
        //    }
        //    else if (MakeFolderForLoser)
        //    {
        //        PlayerNames.AddRange(ExtractPlayers(PlayerType.Loser, Losers));
        //    }

        //    // create sort directory, and directories for each player, depending on arguments
        //    string sortDirectory = CurrentDirectory + @"\" + SortCriteria.ToString();
        //    CreateDirectory(sortDirectory);

        //    foreach (var player in PlayerNames/*.Distinct()*/)
        //    {
        //        Directory.CreateDirectory(sortDirectory + @"\" + player);
        //    }

        //    // now add all replays associated with player into the folder

        //    foreach (var replay in ListReplays)
        //    {
        //        // get players per replay
        //        var ParsePlayers = replay.Players.ToList();
        //        var index = ListReplays.IndexOf(replay);
        //        var FilePath = Files.ElementAt(index);
        //        var DirectoryName = Directory.GetParent(FilePath);
        //        var FileName = FilePath.Substring(DirectoryName.ToString().Length);

        //        // is there a way to write this better? I'm purposely putting these MakeFolderForWinner and MakeFolderForLoser checks on the outside, because I think
        //        // having to check 2 boolean values for each player per replay, is too expensive compared to just checking it per replay? or is this not the case..
        //        if (MakeFolderForWinner && MakeFolderForLoser)
        //        {
        //            try
        //            {
        //                foreach (var aPlayer in ParsePlayers)
        //                {
        //                    // for each player, get proper folder
        //                    // find the corresponding replay file
        //                    // add this file to that folder
        //                    var PlayerName = aPlayer.Name;
        //                    var DestinationFilePath = sortDirectory + @"\" + PlayerName + FileName;
        //                    if (!KeepOriginalReplayNames)
        //                    {
        //                        DestinationFilePath = sortDirectory + @"\" + PlayerName + @"\" + ReplayHandler.GenerateReplayName(replay, CustomReplayFormat) + ".rep";
        //                    }
        //                    try
        //                    {
        //                        while (File.Exists(DestinationFilePath))
        //                        {
        //                            DestinationFilePath = AdjustName(DestinationFilePath, false);
        //                        }
        //                        File.Copy(FilePath, DestinationFilePath);
        //                    }
        //                    catch (IOException IOex)
        //                    {
        //                        Console.WriteLine(IOex.Message);
        //                    }
        //                    catch (NotSupportedException NSE)
        //                    {
        //                        Console.WriteLine(NSE.Message);
        //                    }
        //                }
        //            }
        //            catch (Exception /*ex*/)
        //            {
        //                //Console.WriteLine(ex.Message);
        //                Console.WriteLine("Problem with replay: {0}", FilePath);
        //            }
        //        }
        //        else if (MakeFolderForWinner)
        //        {
        //            try
        //            {
        //                foreach (var player in replay.Winner)
        //                {
        //                    var PlayerName = player.Name;
        //                    var DestinationFilePath = sortDirectory + @"\" + PlayerName + FileName;
        //                    if (!KeepOriginalReplayNames)
        //                    {
        //                        DestinationFilePath = sortDirectory + @"\" + PlayerName + @"\" + ReplayHandler.GenerateReplayName(replay, CustomReplayFormat) + ".rep";
        //                    }
        //                    try
        //                    {
        //                        while (File.Exists(DestinationFilePath))
        //                        {
        //                            DestinationFilePath = AdjustName(DestinationFilePath, false);
        //                        }
        //                        File.Copy(FilePath, DestinationFilePath);
        //                    }
        //                    catch (IOException IOex)
        //                    {
        //                        Console.WriteLine(IOex.Message);
        //                    }
        //                    catch (NotSupportedException NSE)
        //                    {
        //                        Console.WriteLine(NSE.Message);
        //                    }
        //                }

        //            }
        //            // how are multiple players recorded in 2v2, if it's just replay.Winner instead of replay.Winners
        //            catch (Exception /*ex*/)
        //            {
        //                Console.WriteLine("Cannot create folder since replay has no winner.");
        //                //Console.WriteLine(ex.Message);
        //            }
        //        }
        //        else if (MakeFolderForLoser)
        //        {
        //            try
        //            {
        //                if (replay.Winner.Count() != 0)
        //                {
        //                    foreach (var aPlayer in ParsePlayers)
        //                    {
        //                        if (/*aPlayer != replay.Winner*/!replay.Winner.Contains(aPlayer))
        //                        {
        //                            var PlayerName = aPlayer.Name;
        //                            var DestinationFilePath = sortDirectory + @"\" + PlayerName + FileName;
        //                            if (!KeepOriginalReplayNames)
        //                            {
        //                                DestinationFilePath = sortDirectory + @"\" + PlayerName + @"\" + ReplayHandler.GenerateReplayName(replay, CustomReplayFormat) + ".rep";
        //                            }
        //                            try
        //                            {
        //                                while (File.Exists(DestinationFilePath))
        //                                {
        //                                    DestinationFilePath = AdjustName(DestinationFilePath, false);
        //                                }
        //                                File.Copy(FilePath, DestinationFilePath);
        //                            }
        //                            catch (IOException IOex)
        //                            {
        //                                Console.WriteLine(IOex.Message);
        //                            }
        //                            catch (NotSupportedException NSE)
        //                            {
        //                                Console.WriteLine(NSE.Message);
        //                            }
        //                        }
        //                    }
        //                }
        //                else
        //                {
        //                    Console.WriteLine("No winner.");
        //                    foreach (var aPlayer in ParsePlayers)
        //                    {
        //                        var PlayerName = aPlayer.Name;
        //                        var DestinationFilePath = sortDirectory + @"\" + PlayerName + FileName;
        //                        if (!KeepOriginalReplayNames)
        //                        {
        //                            DestinationFilePath = sortDirectory + @"\" + PlayerName + @"\" + ReplayHandler.GenerateReplayName(replay, CustomReplayFormat) + ".rep";
        //                        }
        //                        try
        //                        {
        //                            while (File.Exists(DestinationFilePath))
        //                            {
        //                                DestinationFilePath = AdjustName(DestinationFilePath, false);
        //                            }
        //                            File.Copy(FilePath, DestinationFilePath);
        //                        }
        //                        catch (IOException IOex)
        //                        {
        //                            Console.WriteLine(IOex.Message);
        //                        }
        //                        catch (NotSupportedException NSE)
        //                        {
        //                            Console.WriteLine(NSE.Message);
        //                        }
        //                    }
        //                }
        //            }
        //            catch (Exception /*ex*/)
        //            {
        //                // some replays don't have a leave action? Maybe game that crashed or something??
        //                //Console.WriteLine(ex.Message);
        //                Console.WriteLine("Problem with replay: {0}", FilePath);
        //            }
        //        }
        //        else
        //        {
        //            try
        //            {
        //                var DestinationFilePath = sortDirectory + FileName;
        //                if (!KeepOriginalReplayNames)
        //                {
        //                    DestinationFilePath = sortDirectory + @"\" + ReplayHandler.GenerateReplayName(replay, CustomReplayFormat) + ".rep";
        //                }
        //                try
        //                {
        //                    while (File.Exists(DestinationFilePath))
        //                    {
        //                        DestinationFilePath = AdjustName(DestinationFilePath, false);
        //                    }
        //                    File.Copy(FilePath, DestinationFilePath);
        //                }
        //                catch (IOException IOex)
        //                {
        //                    Console.WriteLine(IOex.Message);
        //                }
        //                catch (NotSupportedException NSE)
        //                {
        //                    Console.WriteLine(NSE.Message);
        //                }
        //            }
        //            catch (Exception /*ex*/)
        //            {
        //                //Console.WriteLine(ex.Message);
        //                Console.WriteLine("Problem with replay: {0}", FilePath);
        //            }

        //        }
        //        //foreach (var aPlayer in ParsePlayers)
        //        //{
        //        //    try
        //        //    {
        //        //        if (aPlayer != replay.Winner)
        //        //        {
        //        //            var PlayerName = aPlayer.Name;
        //        //            var DestinationFilePath = sortDirectory + @"\" + PlayerName + FileName;
        //        //            File.Copy(FilePath, DestinationFilePath);
        //        //        }
        //        //    }
        //        //catch (Exception ex)
        //        //{
        //        //    Console.WriteLine("No winner.");
        //        //    Console.WriteLine(ex.Message);
        //        //}
        //        //finally
        //        //{
        //        //    var PlayerName = aPlayer.Name;
        //        //    StringBuilder DestinationFilePath = new StringBuilder();
        //        //    DestinationFilePath.Append(sortDirectory + @"\" + PlayerName + FileName);
        //        //    int counter = 1;
        //        //    while (File.Exists(DestinationFilePath.ToString()))
        //        //    {
        //        //        if (counter > 1)
        //        //        {
        //        //            DestinationFilePath.Remove(DestinationFilePath.Length - 7, 3);
        //        //        }
        //        //        DestinationFilePath.Insert(DestinationFilePath.Length - 4, $"({counter})", 1);
        //        //        counter++;
        //        //    }
        //        //    File.Copy(FilePath, DestinationFilePath.ToString());
        //        //}


        //    }
        //}

        //public void SortOnGameType(bool KeepOriginalReplayNames)
        //{
        //    // replays grouped by gametype
        //    var ReplaysByGameTypes = from replay in ListReplays
        //                             group replay by replay.GameType;

        //    // make sortdirectory
        //    string sortDirectory = CurrentDirectory + @"\" + SortCriteria.ToString();
        //    CreateDirectory(sortDirectory);

        //    // make subdirectory per gametype, and put all associated replays into it

        //    foreach (var gametype in ReplaysByGameTypes)
        //    {
        //        var GameType = gametype.Key.ToString();
        //        Directory.CreateDirectory(sortDirectory + @"\" + GameType);

        //        foreach (var replay in gametype)
        //        {
        //            try
        //            {
        //                ReplayHandler.CopyReplay(ListReplays, replay, Files, sortDirectory, GameType, KeepOriginalReplayNames, CustomReplayFormat);
        //                //var index = ListReplays.IndexOf(replay);
        //                //var FilePath = Files.ElementAt(index);
        //                //var DirectoryName = Directory.GetParent(FilePath);
        //                //var FileName = FilePath.Substring(DirectoryName.ToString().Length);
        //                //var DestinationFilePath = sortDirectory + @"\" + GameType + FileName;

        //                //if (!KeepOriginalReplayNames)
        //                //{
        //                //    DestinationFilePath = sortDirectory + @"\" + GameType + @"\" + ReplayHandler.GenerateReplayName(replay, CustomReplayFormat) + ".rep";
        //                //}

        //                //while (File.Exists(DestinationFilePath))
        //                //{
        //                //    DestinationFilePath = AdjustName(DestinationFilePath, false);
        //                //}
        //                //File.Copy(FilePath, DestinationFilePath);
        //            }
        //            catch (IOException IOex)
        //            {
        //                Console.WriteLine(IOex.Message);
        //            }
        //            catch (NotSupportedException NSE)
        //            {
        //                Console.WriteLine(NSE.Message);
        //            }
        //            catch (NullReferenceException nullex)
        //            {
        //                Console.WriteLine(nullex.Message);
        //            }
        //            catch (ArgumentException AEX)
        //            {
        //                Console.WriteLine(AEX.Message);
        //            }
        //        }
        //    }
        //}


        //public void SortOnMatchUp(bool KeepOriginalReplayNames, IDictionary<Entities.GameType, bool> ValidGameTypes)
        //{
        //    // get all matchups from the replays
        //    // allow the ignoring of specific game types

        //    // already feels badly written, very expensive, sigh...
        //    // you could pass the IEquality comparer to the constructor of the dictionary
        //    List<IDictionary<int, IDictionary<RaceType, int>>> MatchUps = new List<IDictionary<int, IDictionary<RaceType, int>>>();
        //    IDictionary<IDictionary<int, IDictionary<RaceType, int>>, IList<Interfaces.IReplay>> ReplayMatchUp = new Dictionary<IDictionary<int, IDictionary<RaceType, int>>, IList<Interfaces.IReplay>>();

        //    foreach (var replay in ListReplays)
        //    {
        //        try
        //        {
        //            if (ValidGameTypes[replay.GameType] == true)
        //            {
        //                MatchUp MatchUp = new MatchUp(replay);
        //                // int => team
        //                IDictionary<int, IDictionary<RaceType, int>> EncodedMatchUp = new Dictionary<int, IDictionary<RaceType, int>>();
        //                int team = 1;
        //                foreach (var RaceCombination in MatchUp.TeamRaces)
        //                {
        //                    var RaceFrequency = EncodeRacesFrequency(RaceCombination);
        //                    EncodedMatchUp.Add(new KeyValuePair<int, IDictionary<RaceType, int>>(team, RaceFrequency));
        //                    team++;
        //                }
        //                MatchUps.Add(EncodedMatchUp);
        //                if (!ReplayMatchUp.ContainsKey(EncodedMatchUp))
        //                {
        //                    ReplayMatchUp.Add(new KeyValuePair<IDictionary<int, IDictionary<RaceType, int>>, IList<Interfaces.IReplay>>(EncodedMatchUp, new List<Interfaces.IReplay> { replay }));
        //                }
        //                else
        //                {
        //                    ReplayMatchUp[EncodedMatchUp].Add(replay);
        //                }
        //            }
        //        }
        //        catch (NullReferenceException nullex)
        //        {
        //            Console.WriteLine(nullex.Message);
        //        }

        //    }

        //    string sortDirectory = CurrentDirectory + @"\" + SortCriteria.ToString();
        //    CreateDirectory(sortDirectory);

        //    MatchUpEqualityComparer MatchUpEq = new MatchUpEqualityComparer();
        //    foreach (var matchup in MatchUps.Distinct(MatchUpEq))
        //    {
        //        // make directory per matchup
        //        var MatchUpName = MatchUpToString(matchup);
        //        Directory.CreateDirectory(sortDirectory + @"\" + MatchUpName);

        //        // write all associated replays to this directory
        //        var MatchUpReplays = ReplayMatchUp[matchup];
        //        foreach (var replay in MatchUpReplays)
        //        {
        //            try
        //            {
        //                ReplayHandler.CopyReplay(ListReplays, replay, Files, sortDirectory, MatchUpName, KeepOriginalReplayNames, CustomReplayFormat);
        //                //var index = ListReplays.IndexOf(replay);
        //                //var FilePath = Files.ElementAt(index);
        //                //var DirectoryName = Directory.GetParent(FilePath);
        //                //var FileName = FilePath.Substring(DirectoryName.ToString().Length);
        //                //var DestinationFilePath = sortDirectory + @"\" + MatchUpName + FileName;

        //                //if (!KeepOriginalReplayNames)
        //                //{
        //                //    DestinationFilePath = sortDirectory + @"\" + MatchUpName + @"\" + ReplayHandler.GenerateReplayName(replay, CustomReplayFormat) + ".rep";
        //                //}

        //                //while (File.Exists(DestinationFilePath))
        //                //{
        //                //    DestinationFilePath = AdjustName(DestinationFilePath, false);
        //                //}
        //                //File.Copy(FilePath, DestinationFilePath);
        //            }
        //            catch (IOException IOex)
        //            {
        //                Console.WriteLine(IOex.Message);
        //            }
        //            catch (NotSupportedException NSE)
        //            {
        //                Console.WriteLine(NSE.Message);
        //            }
        //            catch (NullReferenceException nullex)
        //            {
        //                Console.WriteLine(nullex.Message);
        //            }
        //            catch (ArgumentException AEX)
        //            {
        //                Console.WriteLine(AEX.Message);
        //            }
        //            catch (Exception ex)
        //            {
        //                Console.WriteLine(ex.Message);
        //            }
        //        }
        //    }
        //}

        public IDictionary<RaceType, int> EncodeRacesFrequency(string raceCombination)
        {
            IDictionary<RaceType, int> EncodedRacesFrequency = new Dictionary<RaceType, int>();

            foreach (var Race in Enum.GetNames(typeof(RaceType)))
            {
                int RaceFrequency = raceCombination.Select((r, i) => r == Race.First() ? i : -1).Where(i => i != -1).Count();
                EncodedRacesFrequency.Add(new KeyValuePair<RaceType, int>((RaceType)Enum.Parse(typeof(RaceType), Race), RaceFrequency));
            }
            return EncodedRacesFrequency;
        }

        //public string MatchUpToString(ICollection<IDictionary<RaceType, int>> MatchUpValues)
        //{
        //    StringBuilder MatchUpString = new StringBuilder();
        //    foreach (var team in MatchUpValues)
        //    {
        //        foreach (var Race in team)
        //        {
        //            MatchUpString.Append(Race.Key.ToString().First(), Race.Value);
        //        }
        //        MatchUpString.Append("vs");
        //    }
        //    MatchUpString.Remove(MatchUpString.Length - 2, 2);
        //    return MatchUpString.ToString();
        //}
        public string MatchUpToString(IDictionary<int, IDictionary<RaceType, int>> MatchUpValues)
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
        

        public static char[] InvalidFileChars = Path.GetInvalidPathChars();
        public static char[] InvalidPathChars = Path.GetInvalidPathChars();
        public static char[] InvalidFileCharsAdditional = new char[] { '*', ':'};
        public void SortOnMap(bool KeepOriginalReplayNames)
        {
            // extract maps from replays, try to group the duplicates
            // implement an actual map comparison based on byte array and euclidean distance + thresholds!
            ReplayMapEqualityComparer MapEq = new ReplayMapEqualityComparer();
            IDictionary<IReplayMap, List<IReplay>> Maps = new Dictionary<IReplayMap, List<IReplay>>(MapEq);


            foreach (var replay in ListReplays)
            {
                if (!Maps.Keys.Contains(replay.ReplayMap))
                {
                    Maps.Add(new KeyValuePair<IReplayMap, List<IReplay>>(replay.ReplayMap, new List<IReplay> { replay }));
                }
                //if (!Maps.ContainsKey(replay.ReplayMap))
                //{
                //    Maps.Add(new KeyValuePair<IReplayMap, List<IReplay>>(replay.ReplayMap, new List<IReplay> { replay }));
                //}
                else
                {
                    Maps[replay.ReplayMap].Add(replay);
                }
            }

            string sortDirectory = CurrentDirectory + @"\" + SortCriteria.ToString();
            CreateDirectory(sortDirectory);

            foreach (var map in Maps)
            {
                var MapName = map.Key.MapName;
                foreach (char invalidChar in InvalidFileChars)
                {
                    MapName = MapName.Replace(invalidChar.ToString(), "");
                }
                foreach (char invalidChar in InvalidFileCharsAdditional)
                {
                    MapName = MapName.Replace(invalidChar.ToString(), "");
                }
                try
                {
                    Directory.CreateDirectory(sortDirectory + @"\" + MapName);
                    var MapReplays = Maps[map.Key];
                    foreach (var replay in MapReplays)
                    {
                        try
                        {
                            ReplayHandler.CopyReplay(ListReplays, replay, Files, sortDirectory, MapName, KeepOriginalReplayNames, CustomReplayFormat);
                            //var index = ListReplays.IndexOf(replay);
                            //var FilePath = Files.ElementAt(index);
                            //var DirectoryName = Directory.GetParent(FilePath);
                            //var FileName = FilePath.Substring(DirectoryName.ToString().Length);
                            //var DestinationFilePath = sortDirectory + @"\" + MapName + FileName;

                            //if (!KeepOriginalReplayNames)
                            //{
                            //    DestinationFilePath = sortDirectory + @"\" + MapName + @"\" + ReplayHandler.GenerateReplayName(replay, CustomReplayFormat) + ".rep";
                            //}

                            //while (File.Exists(DestinationFilePath))
                            //{
                            //    DestinationFilePath = AdjustName(DestinationFilePath, false);
                            //}
                            //File.Copy(FilePath, DestinationFilePath);
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
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    //Console.WriteLine(MapName);
                }
            }
        }

        // durations = sorted array. First range is 0 - duration1, 2nd is from duration1 to duration2,... last one is from durationx to infinity
        //private void SortOnDuration(bool KeepOriginalReplayNames, int[] durations)
        //{
        //    if (durations == null)
        //    {
        //        throw new ArgumentException("Duration intervals cannot be null");
        //    }

        //    IDictionary<int, List<IReplay>> ReplayDurations = new Dictionary<int, List<IReplay>>();

        //    foreach (var replay in ListReplays)
        //    {
        //        TimeSpan replayDuration = TimeSpan.FromSeconds((replay.FrameCount / ((double)1000 / 42)));
        //        double replayDurationInMinutes = replayDuration.TotalMinutes;
        //        int durationInterval = 0;
        //        while (replayDurationInMinutes > durations[durationInterval])
        //        {
        //            durationInterval++;
        //            if (durationInterval == durations.Length)
        //            {
        //                break;
        //            }
        //        }

        //        if (durationInterval != durations.Length)
        //        {
        //            if (!ReplayDurations.ContainsKey(durations[durationInterval]))
        //            {
        //                ReplayDurations.Add(new KeyValuePair<int, List<IReplay>>(durations[durationInterval], new List<IReplay> { replay }));
        //            }
        //            else
        //            {
        //                ReplayDurations[durations[durationInterval]].Add(replay);
        //            }
        //            // => throws error key does not exist !!! => ReplayDurations[durations[durationInterval]].Add(replay);
        //        }
        //        else
        //        {
        //            if (!ReplayDurations.ContainsKey(-1))
        //            {
        //                ReplayDurations.Add(new KeyValuePair<int, List<IReplay>>(-1, new List<IReplay> { replay }));
        //            }
        //            else
        //            {
        //                ReplayDurations[-1].Add(replay);
        //            }

        //        }
        //    }

        //    string sortDirectory = CurrentDirectory + @"\" + SortCriteria.ToString();
        //    CreateDirectory(sortDirectory);

        //    foreach (var durationInterval in ReplayDurations)
        //    {
        //        string DurationName = null;
        //        if (durationInterval.Key != -1)
        //        {
        //            string previousDuration = null;
        //            int DurationIndex = GetFirstIndex(durations, durationInterval.Key);
        //            if (DurationIndex != 0)
        //            {
        //                previousDuration = durations[DurationIndex - 1].ToString() + "m";
        //            }
        //            else
        //            {
        //                previousDuration = "0m";
        //            }
        //            DurationName = previousDuration + "-" + durationInterval.Key.ToString() + "m";
        //        }
        //        else
        //        {
        //            DurationName = durations[durations.Length - 1].ToString() + "m++";
        //        }
        //        try
        //        {
        //            Directory.CreateDirectory(sortDirectory + @"\" + DurationName);
        //            var DurationReplays = ReplayDurations[durationInterval.Key];
        //            foreach (var replay in DurationReplays)
        //            {
        //                try
        //                {
        //                    ReplayHandler.CopyReplay(ListReplays, replay, Files, sortDirectory, DurationName, KeepOriginalReplayNames, CustomReplayFormat);
        //                    //var index = ListReplays.IndexOf(replay);
        //                    //var FilePath = Files.ElementAt(index);
        //                    //var DirectoryName = Directory.GetParent(FilePath);
        //                    //var FileName = FilePath.Substring(DirectoryName.ToString().Length);
        //                    //var DestinationFilePath = sortDirectory + @"\" + DurationName + FileName;

        //                    //if (!KeepOriginalReplayNames)
        //                    //{
        //                    //    DestinationFilePath = sortDirectory + @"\" + DurationName + @"\" + ReplayHandler.GenerateReplayName(replay, CustomReplayFormat) + ".rep";
        //                    //}

        //                    //while (File.Exists(DestinationFilePath))
        //                    //{
        //                    //    DestinationFilePath = AdjustName(DestinationFilePath, false);
        //                    //}
        //                    //File.Copy(FilePath, DestinationFilePath);
        //                }
        //                catch (IOException IOex)
        //                {
        //                    Console.WriteLine(IOex.Message);
        //                }
        //                catch (NotSupportedException NSE)
        //                {
        //                    Console.WriteLine(NSE.Message);
        //                }
        //                catch (NullReferenceException nullex)
        //                {
        //                    Console.WriteLine(nullex.Message);
        //                }
        //                catch (ArgumentException AEX)
        //                {
        //                    Console.WriteLine(AEX.Message);
        //                }
        //                catch (Exception ex)
        //                {
        //                    Console.WriteLine(ex.Message);
        //                }
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            Console.WriteLine(ex.Message);
        //            //Console.WriteLine(MapName);
        //        }
        //    }

        //}

        public int GetFirstIndex(int[] array, int number)
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

        //private string FullName(string abbreviatedRaces)
        //{
        //    // maybe i should work with a stringbuilder somehow?
        //    string FullNameRaces = abbreviatedRaces;
        //    foreach (var Race in Enum.GetNames(typeof(RaceType)))
        //    {
        //        int[] indexes = FullNameRaces.Select((r, i) => r == Race.First() ? i : -1).Where(i => i != -1).ToArray();
        //        int numberOfInsertions = 0;
        //        int numberOfCharsRace = Race.Length;
        //        var RaceMinusFirstLetter = Race.Remove(0, 1);
        //        foreach (var index in indexes)
        //        {
        //            FullNameRaces = FullNameRaces.Insert(((index + 1) + numberOfInsertions * numberOfCharsRace), RaceMinusFirstLetter);
        //        }
        //    }
        //    return FullNameRaces;
        //}

        // is there a better way to do this, than to write 3 functions with slightly different functionality??

        public List<string> ExtractPlayers(PlayerType playertype, List<string> players)
        {
            if (playertype.HasFlag(PlayerType.Winner))
            {
                foreach (var replay in ListReplays)
                {
                    var parseplayers = replay.Players.ToList();
                    foreach (var aplayer in parseplayers)
                    {
                        // checking a list for each replay is slow... maybe define a Dictionary instead??
                        // I really think I need a player class with match history, wins/losses/...
                        if (!players.Contains(aplayer.Name))
                        {
                            try
                            {
                                if (replay.Winner.Contains(aplayer))
                                {
                                    players.Add(aplayer.Name);
                                }
                            }
                            catch (Exception /*ex*/)
                            {
                                Console.WriteLine("No winner.");
                                //Console.WriteLine(ex.Message);
                            }
                        }
                    }
                }
            }
            if (playertype.HasFlag(PlayerType.Loser))
            {
                foreach (var replay in ListReplays)
                {
                    var parseplayers = replay.Players.ToList();
                    foreach (var aplayer in parseplayers)
                    {
                        // checking a list for each replay is slow... maybe define a Dictionary instead??
                        // I really think I need a player class with match history, wins/losses/...
                        if (!players.Contains(aplayer.Name))
                        {
                            try
                            {
                                if (!replay.Winner.Contains(aplayer))
                                {
                                    players.Add(aplayer.Name);
                                }
                            }
                            catch (Exception /*ex*/)
                            {
                                Console.WriteLine("No winner.");
                                //Console.WriteLine(ex.Message);
                            }
                        }
                    }
                }
            }
            if (playertype.HasFlag(PlayerType.Player))
            {
                // without observers..
            }
            if (playertype.HasFlag(PlayerType.All))
            {
                foreach (var replay in ListReplays)
                {
                    var parseplayers = replay.Players.ToList();
                    foreach (var aplayer in parseplayers)
                    {
                        // checking a list for each replay is slow... maybe define a Dictionary instead??
                        // I really think I need a player class with match history, wins/losses/...
                        if (!players.Contains(aplayer.Name))
                        {
                            players.Add(aplayer.Name);
                        }
                    }
                }
            }
            return players;
        }


        //public List<string> ExtractPlayers(List<string> players)
        //{
        //    foreach (var replay in ListReplays)
        //    {
        //        var parseplayers = replay.Players.ToList();
        //        foreach (var aplayer in parseplayers)
        //        {
        //            // checking a list for each replay is slow... maybe define a Dictionary instead??
        //            // I really think I need a player class with match history, wins/losses/...
        //            if (!players.Contains(aplayer.Name))
        //            {
        //                players.Add(aplayer.Name);
        //            }
        //        }
        //    }
        //    return players;
        //}

        //public List<string> ExtractWinners(List<string> players)
        //{
        //    foreach (var replay in ListReplays)
        //    {
        //        var parseplayers = replay.Players.ToList();
        //        foreach (var aplayer in parseplayers)
        //        {
        //            // checking a list for each replay is slow... maybe define a Dictionary instead??
        //            // I really think I need a player class with match history, wins/losses/...
        //            if (!players.Contains(aplayer.Name))
        //            {
        //                try
        //                {
        //                    if (replay.Winner.Contains(aplayer))
        //                    {
        //                        players.Add(aplayer.Name);
        //                    }
        //                }
        //                catch (Exception /*ex*/)
        //                {
        //                    Console.WriteLine("No winner.");
        //                    //Console.WriteLine(ex.Message);
        //                }
        //            }
        //        }
        //    }
        //    return players;
        //}
        //public List<string> ExtractLosers(List<string> players)
        //{
        //    foreach (var replay in ListReplays)
        //    {
        //        var parseplayers = replay.Players.ToList();
        //        foreach (var aplayer in parseplayers)
        //        {
        //            // checking a list for each replay is slow... maybe define a Dictionary instead??
        //            // I really think I need a player class with match history, wins/losses/...
        //            if (!players.Contains(aplayer.Name))
        //            {
        //                try
        //                {
        //                    if (!replay.Winner.Contains(aplayer))
        //                    {
        //                        players.Add(aplayer.Name);
        //                    }
        //                }
        //                catch (Exception /*ex*/)
        //                {
        //                    Console.WriteLine("No winner.");
        //                    //Console.WriteLine(ex.Message);
        //                }
        //            }
        //        }
        //    }
        //    return players;
        //}

        // Make some sort of "Utitility class" for directory checking etc
        // should be able to improve these functions... they start checking from 0 to ... which is not optimal
        //private string DirectoryAdjustName(string directoryName)
        //{
        //    while (Directory.Exists(directoryName))
        //    {
        //        char[] array_DirectoryName = directoryName.ToCharArray();
        //        int indexLastChar = array_DirectoryName.Length - 1;
        //        bool AddOne;
        //        while (IncrementName(ref array_DirectoryName, ref indexLastChar, out AddOne)) ;
        //        if (AddOne)
        //        {
        //            directoryName = new string(array_DirectoryName);
        //            directoryName = directoryName.Insert(indexLastChar + 1,"1");
        //        }
        //        else
        //        {
        //            directoryName = new string(array_DirectoryName);
        //        }
        //    }
        //    return directoryName;
        //}

        //private bool IncrementName(ref char[] name,ref int indexChar, out bool AddOne)
        //{
        //    char charToIncrement = name[indexChar];
        //    int lastDigit;

        //    if (int.TryParse(charToIncrement.ToString(), out lastDigit))
        //    {
        //        if (lastDigit == 9)
        //        {
        //            lastDigit = 0;
        //            name[indexChar] = lastDigit.ToString().First();
        //            indexChar--;
        //            AddOne = true;
        //            return true;
        //        }
        //        else
        //        {
        //            lastDigit++;
        //            name[indexChar] = lastDigit.ToString().First();
        //            AddOne = false;
        //            return false;
        //        }
        //    }
        //    AddOne = true;
        //    return false;
        //}

        public static string AdjustName(string fullPath, bool isDirectory)
        {
            int count = 1;

            string fileNameOnly = Path.GetFileNameWithoutExtension(fullPath);
            string extension = Path.GetExtension(fullPath);
            string path = Path.GetDirectoryName(fullPath);
            string newFullPath = fullPath;

            if (isDirectory)
            {
                while (Directory.Exists(newFullPath))
                {
                    newFullPath = IncrementName(fileNameOnly, extension, path, ref count);
                }
            }
            else
            {
                while (File.Exists(newFullPath))
                {
                    newFullPath = IncrementName(fileNameOnly, extension, path, ref count);
                }
            }
            return newFullPath;
        }

        public static string IncrementName(string fileNameOnly, string extension, string path, ref int count)
        {
            string tempFileName = string.Format("{0}({1})", fileNameOnly, count++);
            return Path.Combine(path, tempFileName + extension);
        }

        public void CreateDirectory(string sortDirectory)
        {
            if (Directory.Exists(sortDirectory))
            {
                Console.WriteLine("Sort directory already exists.");
                Console.WriteLine("Write to same directory? Yes/No.");
                var WriteToSameDirectory = User.AskYesNo();
                if (WriteToSameDirectory.Yes != null)
                {
                    if ((bool)!WriteToSameDirectory.Yes)
                    {
                        sortDirectory = AdjustName(sortDirectory, true);
                        Directory.CreateDirectory(sortDirectory);
                    }
                }
            }
            else
            {
                Directory.CreateDirectory(sortDirectory);
            }
        }

        public static string RemoveInvalidChars(string name)
        {
            foreach (var InvalidChar in InvalidPathChars)
            {
                name = name.Replace(InvalidChar.ToString(), string.Empty);
            }
            foreach (var InvalidChar in InvalidFileChars)
            {
                name = name.Replace(InvalidChar.ToString(), string.Empty);
            }
            foreach (var InvalidChar in InvalidFileCharsAdditional)
            {
                name = name.Replace(InvalidChar.ToString(), string.Empty);
            }
            return name;
        }
    }
}
