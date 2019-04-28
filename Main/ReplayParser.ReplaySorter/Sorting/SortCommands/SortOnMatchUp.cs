using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ReplayParser.ReplaySorter.ReplayRenamer;
using System.IO;
using ReplayParser.Interfaces;
using ReplayParser.ReplaySorter.Diagnostics;
using System.ComponentModel;
using ReplayParser.ReplaySorter.IO;

namespace ReplayParser.ReplaySorter.Sorting.SortCommands
{
    public class SortOnMatchUp : ISortCommand
    {

        #region private

        #region methods

        private IDictionary<RaceType, int> EncodeRacesFrequency(string raceCombination)
        {
            IDictionary<RaceType, int> EncodedRacesFrequency = new Dictionary<RaceType, int>();

            //TODO what about random??
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

        #endregion

        #endregion

        #region public

        #region constructor

        public SortOnMatchUp(SortCriteriaParameters sortcriteriaparameters, bool keeporiginalreplaynames, Sorter sorter)
        {
            SortCriteriaParameters = sortcriteriaparameters;
            KeepOriginalReplayNames = keeporiginalreplaynames;
            Sorter = sorter;
        }

        #endregion

        #region properties

        public bool KeepOriginalReplayNames { get; set; }
        public SortCriteriaParameters SortCriteriaParameters { get; set; }
        public Criteria SortCriteria { get { return Criteria.MATCHUP; } }
        public bool IsNested { get; set; }
        public Sorter Sorter { get; set; }

        #endregion

        #region methods

        public IDictionary<string, List<File<IReplay>>> Sort(List<string> replaysThrowingExceptions)
        {
            // Dictionary<directory, dictionary<file, replay>>
            IDictionary<string, List<File<IReplay>>> DirectoryFileReplay = new Dictionary<string, List<File<IReplay>>>();

            // get all matchups from the replays
            // allow the ignoring of specific game types

            // you could pass the IEquality comparer to the constructor of the dictionary
            MatchUpEqualityComparer MatchUpEq = new MatchUpEqualityComparer();
            //IDictionary<int, IDictionary<RaceType, int>> MatchUps = new Dictionary<int, IDictionary<RaceType, int>>();
            IDictionary<IDictionary<int, IDictionary<RaceType, int>>, IList<File<IReplay>>> ReplayMatchUps = new Dictionary<IDictionary<int, IDictionary<RaceType, int>>, IList<File<IReplay>>>(MatchUpEq);

            foreach (var replay in Sorter.ListReplays)
            {
                try
                {
                    if (SortCriteriaParameters.ValidGameTypes[replay.Content.GameType] == true)
                    {
                        Teams Team = new Teams(replay.Content);
                        MatchUp MatchUp = new MatchUp(replay.Content, Team);
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
                        if (!ReplayMatchUps.ContainsKey(EncodedMatchUp))
                        {
                            ReplayMatchUps.Add(new KeyValuePair<IDictionary<int, IDictionary<RaceType, int>>, IList<File<IReplay>>>(EncodedMatchUp, new List<File<IReplay>> { replay }));
                        }
                        else
                        {
                            ReplayMatchUps[EncodedMatchUp].Add(replay);
                        }
                    }
                }
                catch (Exception ex)
                {
                    ErrorLogger.GetInstance()?.LogError($"{DateTime.Now} - SortOnMatchUp exception: Encoding matchups, file: {replay.OriginalFilePath}", ex: ex);
                    //Console.WriteLine(nullex.Message);
                }

            }

            string sortDirectory = Sorter.CurrentDirectory;
            if (!(IsNested && !Sorter.GenerateIntermediateFolders))
            {
                if (IsNested)
                {
                    sortDirectory = Sorter.CurrentDirectory + @"\" + SortCriteria;
                }
                else
                {
                    sortDirectory = Sorter.CurrentDirectory + @"\" + string.Join(",", Sorter.CriteriaStringOrder);
                }
                sortDirectory = FileHandler.CreateDirectory(sortDirectory);
            }
            
            foreach (var matchup in /*MatchUps.Distinct(MatchUpEq)*/ReplayMatchUps.Keys)
            {
                // make directory per matchup
                var MatchUpName = MatchUpToString(matchup);
                Directory.CreateDirectory(sortDirectory + @"\" + MatchUpName);

                var FileReplays = new List<File<IReplay>>();
                DirectoryFileReplay.Add(new KeyValuePair<string, List<File<IReplay>>>(sortDirectory + @"\" + MatchUpName, FileReplays));

                // write all associated replays to this directory
                var MatchUpReplays = ReplayMatchUps[matchup];
                foreach (var replay in MatchUpReplays)
                {
                    bool threwException = false;
                    try
                    {
                        if (IsNested == false)
                        {
                            ReplayHandler.CopyReplay(replay, sortDirectory, MatchUpName, KeepOriginalReplayNames, Sorter.CustomReplayFormat);
                        }
                        else
                        {
                            ReplayHandler.MoveReplay(replay, sortDirectory, MatchUpName, KeepOriginalReplayNames, Sorter.CustomReplayFormat);
                        }
                        
                        FileReplays.Add(replay);
                    }
                    catch (IOException IOex)
                    {
                        threwException = true;
                        ErrorLogger.GetInstance()?.LogError($"{DateTime.Now} - SortOnMatchUp IOException", ex: IOex);
                    }
                    catch (NotSupportedException NSE)
                    {
                        threwException = true;
                        ErrorLogger.GetInstance()?.LogError($"{DateTime.Now} - SortOnMatchUp NotSupportedException", ex: NSE);
                    }
                    catch (NullReferenceException nullex)
                    {
                        threwException = true;
                        ErrorLogger.GetInstance()?.LogError($"{DateTime.Now} - SortOnMatchUp NullReferenceException: {replay.OriginalFilePath}", ex: nullex);
                    }
                    catch (ArgumentException AEX)
                    {
                        threwException = true;
                        ErrorLogger.GetInstance()?.LogError($"{DateTime.Now} - SortOnMatchUp ArgumentException", ex: AEX);
                    }
                    catch (Exception ex)
                    {
                        threwException = true;
                        ErrorLogger.GetInstance()?.LogError($"{DateTime.Now} - SortOnMatchUp Exception", ex: ex);
                    }
                    if (threwException)
                        replaysThrowingExceptions.Add(replay.OriginalFilePath);
                }
            }
            // not implemented yet
            return DirectoryFileReplay;
        }

        public IDictionary<string, List<File<IReplay>>> SortAsync(List<string> replaysThrowingExceptions, BackgroundWorker worker_ReplaySorter, int currentCriteria, int numberOfCriteria, int currentPositionNested, int numberOfPositions)
        {
            // Dictionary<directory, dictionary<file, replay>>
            IDictionary<string, List<File<IReplay>>> DirectoryFileReplay = new Dictionary<string, List<File<IReplay>>>();

            // get all matchups from the replays
            // allow the ignoring of specific game types

            // you could pass the IEquality comparer to the constructor of the dictionary
            MatchUpEqualityComparer MatchUpEq = new MatchUpEqualityComparer();
            //IDictionary<int, IDictionary<RaceType, int>> MatchUps = new Dictionary<int, IDictionary<RaceType, int>>();
            IDictionary<IDictionary<int, IDictionary<RaceType, int>>, IList<File<IReplay>>> ReplayMatchUp = new Dictionary<IDictionary<int, IDictionary<RaceType, int>>, IList<File<IReplay>>>(MatchUpEq);

            foreach (var replay in Sorter.ListReplays)
            {
                try
                {
                    if (SortCriteriaParameters.ValidGameTypes[replay.Content.GameType] == true)
                    {
                        Teams Team = new Teams(replay.Content);
                        MatchUp MatchUp = new MatchUp(replay.Content, Team);
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
                            ReplayMatchUp.Add(new KeyValuePair<IDictionary<int, IDictionary<RaceType, int>>, IList<File<IReplay>>>(EncodedMatchUp, new List<File<IReplay>> { replay }));
                        }
                        else
                        {
                            ReplayMatchUp[EncodedMatchUp].Add(replay);
                        }
                    }
                }
                catch (Exception ex)
                {
                    ErrorLogger.GetInstance()?.LogError($"{DateTime.Now} - SortOnMatchUp exception: Encoding matchups, file: {replay.OriginalFilePath}", ex: ex);
                }

            }

            string sortDirectory = Sorter.CurrentDirectory;
            if (!(IsNested && !Sorter.GenerateIntermediateFolders))
            {
                if (IsNested)
                {
                    sortDirectory = Sorter.CurrentDirectory + @"\" + SortCriteria;
                }
                else
                {
                    sortDirectory = Sorter.CurrentDirectory + @"\" + string.Join(",", Sorter.CriteriaStringOrder);
                }
                sortDirectory = FileHandler.CreateDirectory(sortDirectory);
            }

            int currentPosition = 0;
            int progressPercentage = 0;

            foreach (var matchup in ReplayMatchUp.Keys)
            {
                // make directory per matchup
                var MatchUpName = MatchUpToString(matchup);
                Directory.CreateDirectory(sortDirectory + @"\" + MatchUpName);

                var FileReplays = new List<File<IReplay>>();
                DirectoryFileReplay.Add(new KeyValuePair<string, List<File<IReplay>>>(sortDirectory + @"\" + MatchUpName, FileReplays));

                // write all associated replays to this directory
                var MatchUpReplays = ReplayMatchUp[matchup];
                foreach (var replay in MatchUpReplays)
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
                    worker_ReplaySorter.ReportProgress(progressPercentage, "sorting on matchup...");
                    try
                    {
                        if (IsNested == false)
                        {
                            ReplayHandler.CopyReplay(replay, sortDirectory, MatchUpName, KeepOriginalReplayNames, Sorter.CustomReplayFormat);
                        }
                        else
                        {
                            ReplayHandler.MoveReplay(replay, sortDirectory, MatchUpName, KeepOriginalReplayNames, Sorter.CustomReplayFormat);
                        }

                        FileReplays.Add(replay);
                    }
                    catch (IOException IOex)
                    {
                        threwException = true;
                        ErrorLogger.GetInstance()?.LogError($"{DateTime.Now} - SortOnMatchUp IOException", ex: IOex);
                    }
                    catch (NotSupportedException NSE)
                    {
                        threwException = true;
                        ErrorLogger.GetInstance()?.LogError($"{DateTime.Now} - SortOnMatchUp NotSupportedException", ex: NSE);
                    }
                    catch (NullReferenceException nullex)
                    {
                        threwException = true;
                        ErrorLogger.GetInstance()?.LogError($"{DateTime.Now} - SortOnMatchUp NullReferenceException: {replay.OriginalFilePath}", ex: nullex);
                    }
                    catch (ArgumentException AEX)
                    {
                        threwException = true;
                        ErrorLogger.GetInstance()?.LogError($"{DateTime.Now} - SortOnMatchUp ArgumentException", ex: AEX);
                    }
                    catch (Exception ex)
                    {
                        threwException = true;
                        ErrorLogger.GetInstance()?.LogError($"{DateTime.Now} - SortOnMatchUp Exception", ex: ex);
                    }
                    if (threwException)
                    {
                        replaysThrowingExceptions.Add(replay.OriginalFilePath);
                    }
                }
            }
            // not implemented yet
            return DirectoryFileReplay;
        }

        //TODO if you want you could incorporate preview into the regular sort methods, but it's annoying to adjust
        public IDictionary<string, List<File<IReplay>>> PreviewSort(List<string> replaysThrowingExceptions, BackgroundWorker worker_ReplaySorter, int currentCriteria, int numberOfCriteria, int currentPositionNested = 0, int numberOfPositions = 0)
        {
            IDictionary<string, List<File<IReplay>>> DirectoryFileReplay = new Dictionary<string, List<File<IReplay>>>();

            MatchUpEqualityComparer MatchUpEq = new MatchUpEqualityComparer();
            IDictionary<IDictionary<int, IDictionary<RaceType, int>>, IList<File<IReplay>>> ReplayMatchUp = new Dictionary<IDictionary<int, IDictionary<RaceType, int>>, IList<File<IReplay>>>(MatchUpEq);

            foreach (var replay in Sorter.ListReplays)
            {
                try
                {
                    if (SortCriteriaParameters.ValidGameTypes[replay.Content.GameType] == true)
                    {
                        Teams Team = new Teams(replay.Content);
                        MatchUp MatchUp = new MatchUp(replay.Content, Team);
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
                            ReplayMatchUp.Add(new KeyValuePair<IDictionary<int, IDictionary<RaceType, int>>, IList<File<IReplay>>>(EncodedMatchUp, new List<File<IReplay>> { replay }));
                        }
                        else
                        {
                            ReplayMatchUp[EncodedMatchUp].Add(replay);
                        }
                    }
                }
                catch (Exception ex)
                {
                    ErrorLogger.GetInstance()?.LogError($"{DateTime.Now} - SortOnMatchUp exception: Encoding matchups, file: {replay.OriginalFilePath}", ex: ex);
                }

            }

            string sortDirectory = Sorter.CurrentDirectory;
            if (!(IsNested && !Sorter.GenerateIntermediateFolders))
            {
                if (IsNested)
                {
                    sortDirectory = Sorter.CurrentDirectory + @"\" + SortCriteria;
                }
                else
                {
                    sortDirectory = Sorter.CurrentDirectory + @"\" + string.Join(",", Sorter.CriteriaStringOrder);
                }
                sortDirectory = FileHandler.AdjustName(sortDirectory, true);
            }

            int currentPosition = 0;
            int progressPercentage = 0;

            foreach (var matchup in ReplayMatchUp.Keys)
            {
                var MatchUpName = MatchUpToString(matchup);
                var FileReplays = new List<File<IReplay>>();
                DirectoryFileReplay.Add(new KeyValuePair<string, List<File<IReplay>>>(sortDirectory + @"\" + MatchUpName, FileReplays));
                var MatchUpReplays = ReplayMatchUp[matchup];

                foreach (var replay in MatchUpReplays)
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
                    worker_ReplaySorter.ReportProgress(progressPercentage, "sorting on matchup...");
                    try
                    {
                        if (IsNested == false)
                        {
                            ReplayHandler.CopyReplay(replay, sortDirectory, MatchUpName, KeepOriginalReplayNames, Sorter.CustomReplayFormat, true);
                        }
                        else
                        {
                            ReplayHandler.MoveReplay(replay, sortDirectory, MatchUpName, KeepOriginalReplayNames, Sorter.CustomReplayFormat, true);
                        }

                        FileReplays.Add(replay);
                    }
                    catch (IOException IOex)
                    {
                        threwException = true;
                        ErrorLogger.GetInstance()?.LogError($"{DateTime.Now} - SortOnMatchUp IOException", ex: IOex);
                    }
                    catch (NotSupportedException NSE)
                    {
                        threwException = true;
                        ErrorLogger.GetInstance()?.LogError($"{DateTime.Now} - SortOnMatchUp NotSupportedException", ex: NSE);
                    }
                    catch (NullReferenceException nullex)
                    {
                        threwException = true;
                        ErrorLogger.GetInstance()?.LogError($"{DateTime.Now} - SortOnMatchUp NullReferenceException: {replay.OriginalFilePath}", ex: nullex);
                    }
                    catch (ArgumentException AEX)
                    {
                        threwException = true;
                        ErrorLogger.GetInstance()?.LogError($"{DateTime.Now} - SortOnMatchUp ArgumentException", ex: AEX);
                    }
                    catch (Exception ex)
                    {
                        threwException = true;
                        ErrorLogger.GetInstance()?.LogError($"{DateTime.Now} - SortOnMatchUp Exception", ex: ex);
                    }
                    if (threwException)
                    {
                        replaysThrowingExceptions.Add(replay.OriginalFilePath);
                    }
                }
            }
            // not implemented yet
            return DirectoryFileReplay;
        }

        #endregion

        #endregion
    }
}
