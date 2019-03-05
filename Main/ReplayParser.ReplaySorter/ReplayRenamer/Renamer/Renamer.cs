using ReplayParser.Interfaces;
using ReplayParser.ReplaySorter.Diagnostics;
using ReplayParser.ReplaySorter.Sorting.SortResult;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace ReplayParser.ReplaySorter.ReplayRenamer
{
    public class Renamer
    {
        private RenamingParameters _renamingParameters;
        private List<File<IReplay>> _listReplays;

        public Renamer(RenamingParameters renamingParameters, List<File<IReplay>> listReplays)
        {
            _renamingParameters = renamingParameters;
            _listReplays = listReplays;
        }

        public string OriginalDirectory => _renamingParameters.OriginalDirectory;
        public bool RenameInPlace => _renamingParameters.RenameInPlace;
        public bool RenameLastSort => _renamingParameters.RenameLastSort;
        public string OutputDirectory => _renamingParameters.OutputDirectory;
        public CustomReplayFormat CustomReplayFormat => _renamingParameters.CustomReplayFormat;
        public IEnumerable<File<IReplay>> Replays => _listReplays.AsEnumerable();

        // how to avoid coupling yourself to backgroundworker...
        public ServiceResult RenameInPlaceAsync(BackgroundWorker worker_ReplayRenamer, bool ignoreSorted)
        {
            var firstReplay = _listReplays.FirstOrDefault();
            if (firstReplay == null)
            {
                return new ServiceResult(string.Empty, false, new List<string> { "You have to parse replays before you can sort. Replay list is empty!" });
            }

            if (!ignoreSorted && (firstReplay.OriginalFileName != firstReplay.FileName))
            {
                return new ServiceResult(string.Empty, false, new List<string> { "Replays have been sorted already. Please execute restore before attempting to rename in place." });
            }

            Stopwatch sw = new Stopwatch();
            sw.Start();
            int currentPosition = 0;
            int progressPercentage = 0;
            int replaysThrowingExceptions = 0;

            foreach (var replay in _listReplays)
            {
                if (worker_ReplayRenamer.CancellationPending == true)
                {
                    sw.Stop();
                    return new ServiceResult("Renaming cancelled by user...", true, null);
                }

                currentPosition++;
                progressPercentage = Convert.ToInt32(((double)currentPosition / _listReplays.Count()) * 100);
                worker_ReplayRenamer.ReportProgress(progressPercentage);
                try
                {
                    replay.FileName = Directory.GetParent(replay.FileName).ToString() + ReplayHandler.GenerateReplayName(replay.Content, CustomReplayFormat);
                }
                catch (Exception ex)
                {
                    replaysThrowingExceptions++;
                    ErrorLogger.LogError($"Error while renaming replay: {replay.OriginalFileName}", OriginalDirectory + @"\LogErrors", ex);
                }
            }

            sw.Stop();
            return new ServiceResult($"Finished renaming replays! It took {sw.Elapsed} to rename {_listReplays.Count()} replays. {replaysThrowingExceptions} replays encountered exceptions.", true, null);
        }

        // how to avoid coupling yourself to backgroundworker...
        public ServiceResult<DirectoryFileTree<File<IReplay>>> RenameToDirectoryAsync(BackgroundWorker worker_ReplayRenamer)
        {
            //TODO
            if (!Directory.Exists(replayRenamingOutputDirectory))
            {
                MessageBox.Show("The specified directory does not exist.", "Invalid directory", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);
                return;
            }
            foreach (var replay in ListReplays)
            {
                currentPosition++;
                progressPercentage = Convert.ToInt32(((double)currentPosition / files.Count()) * 100);
                (sender as BackgroundWorker).ReportProgress(progressPercentage);
                ReplayHandler.CopyReplay(replay, replayRenamingOutputDirectory, string.Empty, false, customReplayFormat);
            }
        }
    }
}
