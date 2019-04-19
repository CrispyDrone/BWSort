using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using ReplayParser.ReplaySorter.Sorting;
using ReplayParser.Interfaces;
using ReplayParser.ReplaySorter.UserInput;
using ReplayParser.ReplaySorter.Sorting.SortCommands;
using System.ComponentModel;
using System.Windows;
using ReplayParser.ReplaySorter.Sorting.SortResult;
using ReplayParser.ReplaySorter.IO;

namespace ReplayParser.ReplaySorter
{
    public class Sorter
    {
        #region private fields

        private SortCommandFactory Factory = new SortCommandFactory();

        #endregion

        #region constructors

        public Sorter() { }

        public Sorter(string originalDirectory, List<File<IReplay>> listreplays)
        {
            this.ListReplays = listreplays;
            this.OriginalListReplays = new List<File<IReplay>>(listreplays);
            this.CurrentDirectory = originalDirectory;
            this.OriginalDirectory = originalDirectory;
        }

        #endregion

        #region public
        #region public properties

        public Criteria SortCriteria { get; set; }

        public string[] CriteriaStringOrder { get; set; }

        public List<File<IReplay>> ListReplays { get; set; }

        public string CurrentDirectory { get; set; }

        public CustomReplayFormat CustomReplayFormat { get; set; }

        public SortCriteriaParameters SortCriteriaParameters { get; set; }

        public string OriginalDirectory { get; }

        public List<File<IReplay>> OriginalListReplays { get; }

        #endregion

        #region public methods

        //TODO sorting needs to start with the original names, and then progressively work with the filename....??
        public void ExecuteSort(SortCriteriaParameters sortcriteriaparameters, bool keeporiginalreplaynames, List<string> replaysThrowingExceptions)
        {
            ReplayHandler.SaveReplayFilePaths(OriginalListReplays);
            // why do i need this silly string array with the original order...
            IDictionary<string, List<File<IReplay>>> SortOnXResult = null;
            for (int i = 0; i < CriteriaStringOrder.Length; i++)
            {
                // should I pass a new sorter instead of this?? Then I don't have to make separate property OriginalDirectory
                var SortOnX = Factory.GetSortCommand((Criteria)Enum.Parse(typeof(Criteria), CriteriaStringOrder[i]), sortcriteriaparameters, keeporiginalreplaynames, this);
                if (i == 0)
                {
                    SortOnXResult = SortOnX.Sort(replaysThrowingExceptions);
                }
                else
                {
                    // nested sort
                    SortOnX.IsNested = true;
                    SortOnXResult = NestedSort(SortOnX, SortOnXResult, replaysThrowingExceptions);
                }
            }
            ReplayHandler.ResetReplayFilePathsToBeforeSort(OriginalListReplays);
        }

        public IDictionary<string, List<File<IReplay>>> NestedSort(ISortCommand SortOnX, IDictionary<string, List<File<IReplay>>> SortOnXResult, List<string> replaysThrowingExceptions)
        {
            // Dictionary<directory, Files>
            IDictionary<string, List<File<IReplay>>> DirectoryFileReplay = new Dictionary<string, List<File<IReplay>>>();

            SortOnX.Sorter.SortCriteria = SortOnX.SortCriteria;
            foreach (var directory in SortOnXResult.Keys)
            {
                var FileReplays = SortOnXResult[directory];
                SortOnX.Sorter.CurrentDirectory = directory;
                SortOnX.Sorter.ListReplays = FileReplays;
                var result = SortOnX.Sort(replaysThrowingExceptions);
                DirectoryFileReplay = DirectoryFileReplay.Concat(result).ToDictionary(k => k.Key, k => k.Value);
            }
            // not implemented yet
            return DirectoryFileReplay;
        }

        public DirectoryFileTree<File<IReplay>> ExecuteSortAsync(bool keeporiginalreplaynames, BackgroundWorker worker_ReplaySorter, List<string> replaysThrowingExceptions)
        {
            ReplayHandler.SaveReplayFilePaths(OriginalListReplays);
            // Sort Result ! 
            // DirectoryFileTree<File<IReplay>> TotalSortResult = new DirectoryFileTree<File<IReplay>>(new DirectoryInfo(OriginalDirectory));

            // why do i need this silly string array with the original order...
            IDictionary<string, List<File<IReplay>>> SortOnXResult = new Dictionary<string, List<File<IReplay>>>();
            for (int i = 0; i < CriteriaStringOrder.Length; i++)
            {
                // should I pass a new sorter instead of this?? Then I don't have to make separate property OriginalDirectory
                var SortOnX = Factory.GetSortCommand((Criteria)Enum.Parse(typeof(Criteria), CriteriaStringOrder[i]), SortCriteriaParameters, keeporiginalreplaynames, this);
                if (i == 0)
                {
                    SortOnXResult = SortOnX.SortAsync(replaysThrowingExceptions, worker_ReplaySorter, i + 1, CriteriaStringOrder.Count());
                    if (worker_ReplaySorter.CancellationPending == true)
                    {
                        return null;
                    }
                    // make separate functions for this
                    // DirectoryFileTree<File<IReplay>> FirstSort = new DirectoryFileTree<File<IReplay>>(new DirectoryInfo(CurrentDirectory + @"\" + SortCriteria.ToString()));
                    // foreach (var directory in SortOnXResult.Keys)
                    // {
                    //     if (FirstSort.Children != null)
                    //     {
                    //         FirstSort.Children.Add(new DirectoryFileTree<File<IReplay>>(new DirectoryInfo(directory), SortOnXResult[directory]));
                    //     }
                    //     else
                    //     {
                    //         FirstSort.Children = new List<DirectoryFileTree<File<IReplay>>>();
                    //         FirstSort.Children.Add(new DirectoryFileTree<File<IReplay>>(new DirectoryInfo(directory), SortOnXResult[directory]));
                    //     }
                    // }
                    // if (TotalSortResult.Children != null)
                    // {
                    //     TotalSortResult.Children.Add(FirstSort);
                    // }
                    // else
                    // {
                    //     TotalSortResult.Children = new List<DirectoryFileTree<File<IReplay>>>();
                    //     TotalSortResult.Children.Add(FirstSort);
                    // }
                    
                }
                else
                {
                    // nested sort
                    SortOnX.IsNested = true;
                    SortOnXResult = NestedSortAsync(replaysThrowingExceptions, SortOnX, SortOnXResult, worker_ReplaySorter, i + 1, CriteriaStringOrder.Count());
                    if (worker_ReplaySorter.CancellationPending == true)
                    {
                        return null;
                    }
                    // make separate functions for this 
                    // adjust the FirstSort... for each child you need to make the changes... inside the actual NestedSort function....
                }

            }
            ReplayHandler.ResetReplayFilePathsToBeforeSort(OriginalListReplays);
            return BuildTree(OriginalListReplays);
            // return TotalSortResult;
        }

        // not async yet
        public IDictionary<string, List<File<IReplay>>> NestedSortAsync(List<string> replaysThrowingExceptions, ISortCommand SortOnX, IDictionary<string, List<File<IReplay>>> SortOnXResult, BackgroundWorker worker_ReplaySorter, int currentCriteria, int numberOfCriteria)
        {
            // Dictionary<directory, dictionary<file, replay>>
            IDictionary<string, List<File<IReplay>>> DirectoryFileReplay = new Dictionary<string, List<File<IReplay>>>();

            // get replays
            // get files
            // set currentdirectory
            // on sorter
            // for replays and files, need to make return type for sort, which gives a dictionary for replays per directory

            SortOnX.Sorter.SortCriteria = SortOnX.SortCriteria;
            int currentPostion = 0;
            int numberOfPositions = SortOnXResult.Keys.Count();
            foreach (var directory in SortOnXResult.Keys)
            {
                currentPostion++;
                var FileReplays = SortOnXResult[directory];
                SortOnX.Sorter.CurrentDirectory = directory;
                SortOnX.Sorter.ListReplays = FileReplays;
                var result = SortOnX.SortAsync(replaysThrowingExceptions, worker_ReplaySorter, currentCriteria, numberOfCriteria, currentPostion, numberOfPositions);
                if (worker_ReplaySorter.CancellationPending == true)
                {
                    return null;
                }

                DirectoryFileReplay = DirectoryFileReplay.Concat(result).ToDictionary(k => k.Key, k => k.Value);
            }
            // not implemented yet
            return DirectoryFileReplay;
        }

        public override string ToString()
        {
            return $"SortCriteria: {string.Join(" ", CriteriaStringOrder)} SortCriteriaParameters: {SortCriteriaParameters.ToString()} CustomReplayFormat: {CustomReplayFormat?.ToString() ?? string.Empty}";
        }

        #endregion
        #endregion

        #region private methods

        private DirectoryFileTree<File<IReplay>> BuildTree(List<File<IReplay>> originalListReplays)
        {
            // var replayFilePaths = originalListReplays.Select(r => r.FilePath);
            var directories = new Dictionary<string, DirectoryFileTree<File<IReplay>>();

            foreach (var replay in originalListReplays)
            {
                var dirs = ExtractDirectoriesFromPath(replay.FilePath, OriginalDirectory);
                int counter = 0;
                string previousDir = string.Empty;
                foreach (var dir in dirs)
                {
                    AddOrModify(directories, dir, dir == Path.GetDirectoryName(replay.FilePath) ? replay : null);
                    if (counter != 0)
                    {
                        directories[previousDir].Children.Add(directories[dir]);
                    }
                    counter++;
                    previousDir = dir;
                }
            }
            return directories[OriginalDirectory];
        }

        /// <summary>
        /// returns ordered enumerable of directories contained in a path
        /// </summary>
        /// <param name="replayFilePath"></param>
        /// <param name="rootDirectory"></param>
        /// <returns></returns>
        private IEnumerable<string> ExtractDirectoriesFromPath(string replayFilePath, string rootDirectory)
        {
            if (string.IsNullOrWhiteSpace(replayFilePath) || string.IsNullOrWhiteSpace(rootDirectory)) yield break;

            var path = replayFilePath.Substring(rootDirectory.Length + 1);
            // return path.Split(Path.DirectorySeparatorChar);
            
           while (path != string.Empty)
           {
                int indexOfSeparator = path.IndexOf(Path.AltDirectorySeparatorChar);
                yield return path.Substring(0, indexOfSeparator);
                path = path.Substring(indexOfSeparator + 1);
           }
        }

        private void AddOrModify(Dictionary<string, DirectoryFileTree<File<IReplay>>> directories, string directory, File<IReplay> replay)
        {
            if (directories.ContainsKey(directory))
            {
                if (replay != null)
                    directories[directory].Files.Add(FileReplay.Create(replay.Content, replay.OriginalFilePath, replay.Hash));
            }
            else
            {
                if (replay != null)
                {
                    directories.Add(directory, new DirectoryFileTree<File<IReplay>>(new DirectoryInfo(directory), new List<File<IReplay>>() { File<IReplay>.Create(replay.Content, replay.OriginalFilePath, replay.Hash) }));
                }
                else
                {
                    directories.Add(directory, new DirectoryFileTree<File<IReplay>>(new DirectoryInfo(directory)));
                }
            }
        }

        #endregion
    }
}
