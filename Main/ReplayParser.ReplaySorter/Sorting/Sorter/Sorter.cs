﻿using System;
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
using System.Text;

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

        public DirectoryFileTree ExecuteSortAsync(bool keeporiginalreplaynames, BackgroundWorker worker_ReplaySorter, List<string> replaysThrowingExceptions)
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
                }

            }
            var tree = BuildTree();
            ReplayHandler.ResetReplayFilePathsToBeforeSort(OriginalListReplays);
            return tree;
        }

        public DirectoryFileTree PreviewSort(bool keepOriginalReplayNames, BackgroundWorker worker_ReplaySorter, List<string> replaysThrowingExceptions)
        {
            ReplayHandler.SaveReplayFilePaths(OriginalListReplays);

            IDictionary<string, List<File<IReplay>>> SortOnXResult = new Dictionary<string, List<File<IReplay>>>();
            for (int i = 0; i < CriteriaStringOrder.Length; i++)
            {
                var SortOnX = Factory.GetSortCommand((Criteria)Enum.Parse(typeof(Criteria), CriteriaStringOrder[i]), SortCriteriaParameters, keepOriginalReplayNames, this);
                if (i == 0)
                {
                    SortOnXResult = SortOnX.PreviewSort(replaysThrowingExceptions, worker_ReplaySorter, i + 1, CriteriaStringOrder.Count());
                    if (worker_ReplaySorter.CancellationPending == true)
                    {
                        return null;
                    }
                }
                else
                {
                    SortOnX.IsNested = true;
                    SortOnXResult = PreviewNestedSort(replaysThrowingExceptions, SortOnX, SortOnXResult, worker_ReplaySorter, i + 1, CriteriaStringOrder.Count());
                    if (worker_ReplaySorter.CancellationPending == true)
                    {
                        return null;
                    }
                }

            }
            var tree = BuildTree();
            ReplayHandler.ResetReplayFilePathsToBeforeSort(OriginalListReplays);
            return tree;
        }

        public DirectoryFileTree ExecuteSortAsync(DirectoryFileTree previewTree, BackgroundWorker worker_ReplaySorter, List<string> replaysThrowingExceptions)
        {
            var resultingTree = new DirectoryFileTree(previewTree.Root.Name);
            Queue<DirectoryFileTreeNode> nodeQueue = new Queue<DirectoryFileTreeNode>();
            nodeQueue.Enqueue(previewTree.Root);

            while (nodeQueue.Count != 0)
            {
                var node = nodeQueue.Dequeue();
                if (node == null)
                    continue;

                if (node.IsDirectory)
                {
                    foreach (var child in node)
                    {
                        nodeQueue.Enqueue(child);
                        if (child.IsDirectory)
                        {
                            //TODO I just noticed this CreateDirectory function actually can send messageboxes to the user lol...
                            var dirName = FileHandler.CreateDirectory(child.Name, true);
                            resultingTree.AddToNode(node, dirName);
                        }
                        else
                        {
                            var fileReplay = FileReplay.Create(node.Value.Content, node.Value.OriginalFilePath, node.Value.Hash);
                            fileReplay.AddAfterCurrent(node.Name);
                            ReplayHandler.CopyReplay(fileReplay, true);
                            resultingTree.AddToNode(node, fileReplay);
                        }
                    }
                }
            }
            return resultingTree;
        }


        public override string ToString()
        {
            return $"SortCriteria: {string.Join(" ", CriteriaStringOrder)} SortCriteriaParameters: {SortCriteriaParameters.ToString()} CustomReplayFormat: {CustomReplayFormat?.ToString() ?? string.Empty}";
        }

        #endregion
        #endregion

        #region private methods

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
            return DirectoryFileReplay;
        }

        // not async yet
        private IDictionary<string, List<File<IReplay>>> NestedSortAsync(List<string> replaysThrowingExceptions, ISortCommand SortOnX, IDictionary<string, List<File<IReplay>>> SortOnXResult, BackgroundWorker worker_ReplaySorter, int currentCriteria, int numberOfCriteria)
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
            return DirectoryFileReplay;
        }

        private IDictionary<string, List<File<IReplay>>> PreviewNestedSort(List<string> replaysThrowingExceptions, ISortCommand SortOnX, IDictionary<string, List<File<IReplay>>> SortOnXResult, BackgroundWorker worker_ReplaySorter, int currentCriteria, int numberOfCriteria)
        {
            IDictionary<string, List<File<IReplay>>> DirectoryFileReplay = new Dictionary<string, List<File<IReplay>>>();

            SortOnX.Sorter.SortCriteria = SortOnX.SortCriteria;
            int currentPostion = 0;
            int numberOfPositions = SortOnXResult.Keys.Count();
            foreach (var directory in SortOnXResult.Keys)
            {
                currentPostion++;
                var FileReplays = SortOnXResult[directory];
                SortOnX.Sorter.CurrentDirectory = directory;
                SortOnX.Sorter.ListReplays = FileReplays;
                var result = SortOnX.PreviewSort(replaysThrowingExceptions, worker_ReplaySorter, currentCriteria, numberOfCriteria, currentPostion, numberOfPositions);
                if (worker_ReplaySorter.CancellationPending == true)
                {
                    return null;
                }

                DirectoryFileReplay = DirectoryFileReplay.Concat(result).ToDictionary(k => k.Key, k => k.Value);
            }
            return DirectoryFileReplay;
        }

        private DirectoryFileTree BuildTree()
        {
            var tree = new DirectoryFileTree(OriginalDirectory);
            var directoryNodes = new Dictionary<string, DirectoryFileTreeNode>();
            directoryNodes.Add(OriginalDirectory, tree.Root);
            var pathBuilder = new StringBuilder();

            foreach (var replay in OriginalListReplays)
            {
                pathBuilder.Append(OriginalDirectory);
                var dirs = ExtractDirectoriesFromPath(replay.FilePath, OriginalDirectory).Select(d => d.Trim(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).TrimEnd(' ')).Where(d => !string.IsNullOrWhiteSpace(d)).ToList();
                string previousDir = string.Empty;
                foreach (var dir in dirs)
                {
                    previousDir = pathBuilder.ToString();
                    pathBuilder.Append(Path.DirectorySeparatorChar + dir);
                    AddOrModify(tree, directoryNodes, pathBuilder.ToString(), previousDir, dir, pathBuilder.ToString() == Path.GetDirectoryName(replay.FilePath) ? replay : null);
                }
                pathBuilder.Clear();
            }
            return tree;
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
                int indexOfSeparator = path.IndexOf(Path.DirectorySeparatorChar);
                if (indexOfSeparator == -1)
                {
                    indexOfSeparator = path.IndexOf(Path.AltDirectorySeparatorChar);

                    if (indexOfSeparator == -1)
                    {
                        yield break;
                    }
                }

                yield return path.Substring(0, indexOfSeparator);
                path = path.Substring(indexOfSeparator + 1);
           }
        }

        private void AddOrModify(DirectoryFileTree tree, Dictionary<string, DirectoryFileTreeNode> directories, string directoryPath, string previousDirectoryPath, string directory, File<IReplay> replay)
        {
            if (directories == null || string.IsNullOrWhiteSpace(directoryPath))
                return;

            if (directories.ContainsKey(directoryPath))
            {
                if (replay != null)
                {
                    tree.AddToNode(directories[directoryPath], FileReplay.Create(replay.Content, replay.OriginalFilePath, replay.Hash));
                }
            }
            else
            {
                if (replay != null)
                {
                    directories.Add(
                        directoryPath,
                        tree.AddToNode(
                            directories[previousDirectoryPath],
                            directory,
                            new List<FileReplay>() { FileReplay.Create(replay.Content, replay.OriginalFilePath, replay.Hash) }.AsEnumerable()
                        )
                    );
                }
                else
                {
                    directories.Add(
                        directoryPath,
                        tree.AddToNode(
                            directories[previousDirectoryPath],
                            directory)
                    );
                }
            }
        }

        #endregion
    }
}
