using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using ReplayParser.ReplaySorter.Sorting;
using ReplayParser.Interfaces;
using ReplayParser.ReplaySorter.Sorting.SortCommands;
using System.ComponentModel;
using ReplayParser.ReplaySorter.Sorting.SortResult;
using ReplayParser.ReplaySorter.IO;
using System.Text;
using ReplayParser.ReplaySorter.Renaming;

namespace ReplayParser.ReplaySorter
{
    public class Sorter
    {
        #region private

        #region fields

        private SortCommandFactory Factory = new SortCommandFactory();

        #endregion

        #region methods

        private IDictionary<string, List<File<IReplay>>> NestedSort(ISortCommand SortOnX, IDictionary<string, List<File<IReplay>>> SortOnXResult, List<string> replaysThrowingExceptions)
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
                var replaysSorted = result.SelectMany(dir => dir.Value);

                if (replaysSorted.Count() != FileReplays.Count())
                {
                    result.Add(directory, FileReplays.Except(replaysSorted).ToList());
                }
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
                var replaysSorted = result.SelectMany(dir => dir.Value);

                if (replaysSorted.Count() != FileReplays.Count())
                {
                    result.Add(directory, FileReplays.Except(replaysSorted).ToList());
                }

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
                var replaysSorted = result.SelectMany(dir => dir.Value);

                if (replaysSorted.Count() != FileReplays.Count())
                {
                    result.Add(directory, FileReplays.Except(replaysSorted).ToList());
                }

                if (worker_ReplaySorter.CancellationPending == true)
                {
                    return null;
                }

                DirectoryFileReplay = DirectoryFileReplay.Concat(result).ToDictionary(k => k.Key, k => k.Value);
            }
            return DirectoryFileReplay;
        }

        private DirectoryFileTree BuildTree(IDictionary<string, List<File<IReplay>>> directoryFiletree)
        {
            var tree = new DirectoryFileTree(OriginalDirectory);
            var directoryNodes = new Dictionary<string, DirectoryFileTreeNode>();
            directoryNodes.Add(OriginalDirectory, tree.Root);
            var pathBuilder = new StringBuilder();

            foreach (var folderReplays in directoryFiletree)
            {
                pathBuilder.Append(OriginalDirectory);
                var dirs = FileHandler.ExtractDirectoriesFromPath(folderReplays.Key + @"\", OriginalDirectory).Select(d => d.Trim(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)).Where(d => !string.IsNullOrWhiteSpace(d)).ToList();
                string previousDir = string.Empty;
                foreach (var dir in dirs)
                {
                    previousDir = pathBuilder.ToString();
                    pathBuilder.Append(Path.DirectorySeparatorChar + dir);
                    if (pathBuilder.ToString() == folderReplays.Key)
                    {
                        foreach (var replay in folderReplays.Value)
                        {
                            AddOrModify(tree, directoryNodes, pathBuilder.ToString(), previousDir, dir, replay);
                        }
                    }
                    else
                    {
                        AddOrModify(tree, directoryNodes, pathBuilder.ToString(), previousDir, dir, null);
                    }
                }
                pathBuilder.Clear();
            }
            return tree;
        }

        //TODO extract to general BuildTree function inside the DirectoryFileTree ?? Because i'm reusing it when inspecting a backup...
        private void AddOrModify(DirectoryFileTree tree, Dictionary<string, DirectoryFileTreeNode> directories, string directoryPath, string previousDirectoryPath, string directory, File<IReplay> replay)
        {
            if (directories == null || string.IsNullOrWhiteSpace(directoryPath))
                return;

            if (directories.ContainsKey(directoryPath))
            {
                if (replay != null)
                {
                    var fileReplay = FileReplay.Create(replay.Content, replay.OriginalFilePath, replay.Hash);
                    fileReplay.AddAfterCurrent(replay.FilePath);
                    fileReplay.Forward();

                    tree.AddToNode(directories[directoryPath], fileReplay);
                }
            }
            else
            {
                if (replay != null)
                {
                    var fileReplay = FileReplay.Create(replay.Content, replay.OriginalFilePath, replay.Hash);
                    fileReplay.AddAfterCurrent(replay.FilePath);
                    fileReplay.Forward();

                    directories.Add(
                        directoryPath,
                        tree.AddToNode(
                            directories[previousDirectoryPath],
                            directory,
                            new List<FileReplay>() { fileReplay }.AsEnumerable()
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

        private bool ReplayNamesHaveNotChanged(DirectoryFileTree previewTree)
        {
            var replayNamesHaveNotChanged = true;
            var enumerator = previewTree.GetBreadthFirstEnumerator();
            while (enumerator.MoveNext() && replayNamesHaveNotChanged)
            {
                if (enumerator.Current.IsDirectory)
                    continue;

                replayNamesHaveNotChanged = enumerator.Current.Name == Path.GetFileName(enumerator.Current.Value.FilePath);
            }
            return replayNamesHaveNotChanged;
        }

        #endregion

        #endregion

        #region constructors

        public Sorter() { }

        public Sorter(string originalDirectory, List<File<IReplay>> listreplays)
        {
            ListReplays = listreplays;
            OriginalListReplays = new List<File<IReplay>>(listreplays);
            CurrentDirectory = originalDirectory;
            OriginalDirectory = originalDirectory;
        }

        #endregion

        #region public
        #region public properties

        public Criteria SortCriteria { get; set; }

        //TODO should be readonly property
        public string[] CriteriaStringOrder { get; set; }

        public List<File<IReplay>> ListReplays { get; set; }

        public string CurrentDirectory { get; set; }

        public CustomReplayFormat CustomReplayFormat { get; set; }

        public SortCriteriaParameters SortCriteriaParameters { get; set; }

        public string OriginalDirectory { get; }

        public List<File<IReplay>> OriginalListReplays { get; }

        public bool GenerateIntermediateFolders { get; set; }

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
                var SortOnX = Factory.GetSortCommand((Criteria)Enum.Parse(typeof(Criteria), CriteriaStringOrder[i]), sortcriteriaparameters, i == CriteriaStringOrder.Length - 1 ? keeporiginalreplaynames : true, this);
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
            ReplayHandler.RestoreToSavedStateAndClearFuture(OriginalListReplays);
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
                var SortOnX = Factory.GetSortCommand((Criteria)Enum.Parse(typeof(Criteria), CriteriaStringOrder[i]), SortCriteriaParameters, i == CriteriaStringOrder.Length - 1 ? keeporiginalreplaynames : true, this);
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
            // lazy...
            worker_ReplaySorter.ReportProgress(100, "Building tree...");
            var tree = BuildTree(SortOnXResult);
            ReplayHandler.RestoreToSavedStateAndClearFuture(OriginalListReplays);
            return tree;
        }

        public DirectoryFileTree PreviewSort(bool keepOriginalReplayNames, BackgroundWorker worker_ReplaySorter, List<string> replaysThrowingExceptions)
        {
            ReplayHandler.SaveReplayFilePaths(OriginalListReplays);

            IDictionary<string, List<File<IReplay>>> SortOnXResult = new Dictionary<string, List<File<IReplay>>>();
            for (int i = 0; i < CriteriaStringOrder.Length; i++)
            {
                var SortOnX = Factory.GetSortCommand((Criteria)Enum.Parse(typeof(Criteria), CriteriaStringOrder[i]), SortCriteriaParameters, i == CriteriaStringOrder.Length - 1 ? keepOriginalReplayNames : true, this);
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
            // lazy...
            worker_ReplaySorter.ReportProgress(100, "Building tree...");
            var tree = BuildTree(SortOnXResult);
            ReplayHandler.RestoreToSavedStateAndClearFuture(OriginalListReplays);
            return tree;
        }

        /// <summary>
        /// Executes the sort contained in a preview tree.
        /// </summary>
        /// <param name="previewTree"></param>
        /// <param name="worker_ReplaySorter"></param>
        /// <param name="replaysThrowingExceptions"></param>
        /// <returns></returns>
        public DirectoryFileTree ExecuteSortAsync(DirectoryFileTree previewTree, BackgroundWorker worker_ReplaySorter, List<string> replaysThrowingExceptions)
        {
            var resultingTree = new DirectoryFileTree(previewTree.Root.Name);
            var previewToResultingTreeNodesMapping = new Dictionary<DirectoryFileTreeNode, DirectoryFileTreeNode>();
            previewToResultingTreeNodesMapping.Add(previewTree.Root, resultingTree.Root);

            var nodeQueue = new Queue<DirectoryFileTreeNode>();
            nodeQueue.Enqueue(previewTree.Root);

            var previewTreeNodeDirectories = new Dictionary<DirectoryFileTreeNode, string>();
            previewTreeNodeDirectories.Add(previewTree.Root, previewTree.Root.Name + @"\");

            var count = previewTree.Count;
            var currentCount = 0;
            while (nodeQueue.Count != 0)
            {
                var previewNode = nodeQueue.Dequeue();
                if (previewNode == null)
                    continue;

                currentCount++;
                worker_ReplaySorter.ReportProgress(Convert.ToInt32((double)currentCount / count * 100), $"Writing preview to disk... {(previewNode.IsDirectory ? $"Creating directory: {previewNode.Name}" : $"Creating replay {previewNode.Name}")}");

                if (previewNode.IsDirectory)
                {
                    foreach (var previewChild in previewNode.Children.ToList())
                    {
                        nodeQueue.Enqueue(previewChild);
                        if (previewChild.IsDirectory)
                        {
                            var dirName = FileHandler.CreateDirectory(previewTreeNodeDirectories[previewNode] + previewChild.Name + @"\");
                            if (!previewTreeNodeDirectories.ContainsKey(previewChild))
                            {
                                previewTreeNodeDirectories.Add(previewChild, dirName);
                            }
                            previewToResultingTreeNodesMapping.Add(previewChild, resultingTree.AddToNode(previewToResultingTreeNodesMapping[previewNode], FileHandler.ExtractDirectoriesFromPath(dirName, resultingTree.Root.Name).Last()));
                        }
                        else
                        {
                            var fileReplay = FileReplay.Create(previewChild.Value.Content, previewChild.Value.OriginalFilePath, previewChild.Value.Hash);
                            var filePath = previewTreeNodeDirectories[previewNode] + previewChild.Name;
                            fileReplay.AddAfterCurrent(filePath);
                            ReplayHandler.CopyReplay(fileReplay, true);
                            previewToResultingTreeNodesMapping.Add(previewChild, resultingTree.AddToNode(previewToResultingTreeNodesMapping[previewNode], fileReplay));
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

        public bool MatchesInput(DirectoryFileTree previewTree, Tuple<string[], SortCriteriaParameters, CustomReplayFormat, List<File<IReplay>>> previewSortArguments)
        {
            // criteria string order needs to be identical
            // sortcriteriaparameters need to be identical
            // customreplayformat needs to be identical
            // replay set needs to be identical
            // replays in the preview tree need to still have the same name as they currently do, if keeporiginalnames was true
            if (previewSortArguments.Item3 == null)
            {
                return
                    CriteriaStringOrder.SequenceEqual(previewSortArguments.Item1) &&
                    SortCriteriaParameters == previewSortArguments.Item2 &&
                    CustomReplayFormat == previewSortArguments.Item3 &&
                    ListReplays.SequenceEqual(previewSortArguments.Item4) &&
                    ReplayNamesHaveNotChanged(previewTree);
            }
            else
            {
                return
                    CriteriaStringOrder.SequenceEqual(previewSortArguments.Item1) &&
                    SortCriteriaParameters == previewSortArguments.Item2 &&
                    CustomReplayFormat == previewSortArguments.Item3 &&
                    ListReplays.SequenceEqual(previewSortArguments.Item4);
            }
        }

        #endregion

        #endregion

    }
}
