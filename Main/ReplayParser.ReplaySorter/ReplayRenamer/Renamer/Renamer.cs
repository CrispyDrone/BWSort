using ReplayParser.Interfaces;
using ReplayParser.ReplaySorter.Diagnostics;
using ReplayParser.ReplaySorter.IO;
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
        #region private

        #region fields

        private RenamingParameters _renamingParameters;
        private List<File<IReplay>> _listReplays;

        #endregion

        #region methods

        private ServiceResult<ServiceResultSummary> ComputeNames(BackgroundWorker worker_ReplayRenamer, bool ignoreSorted, string outputDirectory, bool restore = false)
        {
            worker_ReplayRenamer.ReportProgress(0, "Computing names...");
            // report progress... with "Generating names..."
            var firstReplay = _listReplays.FirstOrDefault();
            if (firstReplay == null)
            {
                return new ServiceResult<ServiceResultSummary>(ServiceResultSummary.Default, false, new List<string> { "You have to parse replays before you can sort. Replay list is empty!" });
            }

            //TODO error prone since it could be that the first replay fails to be moved/copied during sorting...
            if (!ignoreSorted && (firstReplay.OriginalFilePath != firstReplay.FilePath))
            {
                return new ServiceResult<ServiceResultSummary>(ServiceResultSummary.Default, false, new List<string> { "Replays have been sorted already. Please execute restore before attempting to rename in place. This will make it impossible to rename your last sort." });
            }

            if (!string.IsNullOrWhiteSpace(outputDirectory) && !Directory.Exists(outputDirectory))
            {
                return new ServiceResult<ServiceResultSummary>(ServiceResultSummary.Default, false, new List<string> { "Output directory does not exist." });
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
                    return new ServiceResult<ServiceResultSummary>(new ServiceResultSummary(string.Empty, "Renaming cancelled by user...", sw.Elapsed, currentPosition, replaysThrowingExceptions), true, null);
                }

                currentPosition++;
                progressPercentage = Convert.ToInt32(((double)currentPosition / (_listReplays.Count() * 2)) * 100);
                worker_ReplayRenamer.ReportProgress(progressPercentage);
                try
                {
                    replay.FutureFilePath = (string.IsNullOrWhiteSpace(outputDirectory) ? Directory.GetParent(replay.FilePath).ToString() : outputDirectory ) + @"\" + (restore ?  FileHandler.GetFileName(replay.OriginalFilePath) : ReplayHandler.GenerateReplayName(replay.Content, CustomReplayFormat) + ".rep");
                }
                catch (Exception ex)
                {
                    replaysThrowingExceptions++;
                    //TODO add to serviceresult instead... log later?
                    ErrorLogger.GetInstance()?.LogError($"Error while renaming replay: {replay.OriginalFilePath}", OriginalDirectory + @"\LogErrors", ex);
                }
            }

            sw.Stop();
            return new ServiceResult<ServiceResultSummary>(
                    new ServiceResultSummary(
                        string.Empty, 
                        $"Finished renaming replays! It took {sw.Elapsed} to rename {_listReplays.Count()} replays. {replaysThrowingExceptions} replays encountered exceptions.",
                        sw.Elapsed,
                        currentPosition,
                        replaysThrowingExceptions
                    ),
                    true, 
                    null
                );
        }

        private ServiceResult<ServiceResultSummary> ExecuteRenaming(BackgroundWorker worker_ReplayRenamer, bool shouldCopy)
        {
            worker_ReplayRenamer.ReportProgress(50, "Writing replays...");
            // report progress... with "Copying replays with newly generated names..."
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
                    return new ServiceResult<ServiceResultSummary>
                        (
                            new ServiceResultSummary
                            (
                                string.Empty, 
                                "Renaming cancelled by user...", 
                                sw.Elapsed, 
                                currentPosition, 
                                replaysThrowingExceptions
                            ), 
                            true, 
                            null
                        );
                }

                currentPosition++;
                progressPercentage = 50 + Convert.ToInt32(((double)currentPosition / (_listReplays.Count() * 2)) * 100);
                worker_ReplayRenamer.ReportProgress(progressPercentage);
                try
                {
                    //TODO identical names => failed to create file (add incrementor)
                    if (shouldCopy)
                    {
                        ReplayHandler.CopyReplay(replay); 
                        // File.Copy(replay.OriginalFilePath, replay.FilePath);
                    }
                    else
                    {
                        ReplayHandler.MoveReplay(replay);
                        // File.Move(replay.OriginalFilePath, replay.FilePath);
                    }
                }
                catch (Exception ex)
                {
                    replaysThrowingExceptions++;
                    //TODO add to serviceresult instead... log later?
                    ErrorLogger.GetInstance()?.LogError($"Error while renaming replay: {replay.OriginalFilePath}", OriginalDirectory + @"\LogErrors", ex);
                }
            }

            sw.Stop();
            return new ServiceResult<ServiceResultSummary>
                (
                    new ServiceResultSummary
                    (
                        string.Empty,
                        $"Finished writing replays! It took {sw.Elapsed} to write {_listReplays.Count()} replays. {replaysThrowingExceptions} replays encountered exceptions.",
                        sw.Elapsed,
                        currentPosition,
                        replaysThrowingExceptions
                    ),
                    true,
                    null
                );

        }

        private ServiceResult<ServiceResultSummary> ComputeAndExecuteRenaming(BackgroundWorker worker_ReplayRenamer, bool ignoreSorted, string outputDirectory, bool restore = false)
        {
            var computationResponse = ComputeNames(worker_ReplayRenamer, ignoreSorted, OutputDirectory, restore);

            if (!computationResponse.Success)
            {
                return new ServiceResult<ServiceResultSummary>(ServiceResultSummary.Default, false, new List<string>(computationResponse.Errors));
            }

            var executionResponse = ExecuteRenaming(worker_ReplayRenamer, !string.IsNullOrWhiteSpace(outputDirectory));

            if (!executionResponse.Success)
            {
                return new ServiceResult<ServiceResultSummary>(ServiceResultSummary.Default, false, new List<string>(executionResponse.Errors));
            }

            // combine both responses...
            var combinedDuration = computationResponse.Result.Duration + executionResponse.Result.Duration;
            var combinedErrorCount = computationResponse.Result.ErrorCount + executionResponse.Result.ErrorCount;
            var combinedErrors = computationResponse.Errors == null ? 
                (
                    executionResponse.Errors == null ? Enumerable.Empty<string>() : executionResponse.Errors
                ) : 
                (
                    executionResponse.Errors == null ? computationResponse.Errors : computationResponse.Errors.Concat(executionResponse.Errors)
                );


            return new ServiceResult<ServiceResultSummary>
                (
                    new ServiceResultSummary
                    (
                        string.Empty,
                        $"Finished renaming replays! It took {combinedDuration} to rename and write replays. {combinedErrorCount} exceptions occurred.",
                        combinedDuration,
                        executionResponse.Result.OperationCount,
                        combinedErrorCount
                    ),
                    true,
                    new List<string>(combinedErrors)
                );
        }

        #endregion

        #endregion

        #region public

        #region constructor

        public Renamer(RenamingParameters renamingParameters, List<File<IReplay>> listReplays)
        {
            _renamingParameters = renamingParameters;
            _listReplays = listReplays;
        }

        #endregion

        #region properties

        public string OriginalDirectory => _renamingParameters.OriginalDirectory;
        public bool RenameInPlace => _renamingParameters.RenameInPlace;
        public bool RenameLastSort => _renamingParameters.RenameLastSort;
        public string OutputDirectory => _renamingParameters.OutputDirectory;
        public CustomReplayFormat CustomReplayFormat => _renamingParameters.CustomReplayFormat;
        public IEnumerable<File<IReplay>> Replays => _listReplays.AsEnumerable();

        #endregion

        #region methods

        // how to avoid coupling yourself to backgroundworker...
        // rethink design of not passing parameters, but having them be properties of this renamer class...
        public ServiceResult<ServiceResultSummary> RenameInPlaceAsync(BackgroundWorker worker_ReplayRenamer, bool ignoreSorted)
        {
            return ComputeAndExecuteRenaming(worker_ReplayRenamer, ignoreSorted, string.Empty);
        }

        // how to avoid coupling yourself to backgroundworker...
        // rethink design of not passing parameters, but having them be properties of this renamer class...
        public ServiceResult<ServiceResultSummary> RenameToDirectoryAsync(BackgroundWorker worker_ReplayRenamer)
        {
            return ComputeAndExecuteRenaming(worker_ReplayRenamer, true, OutputDirectory);
        }

        public ServiceResult<ServiceResultSummary> RestoreOriginalNames(BackgroundWorker worker_RenameUndoer)
        {
            // rename in place => names changed => restore => names restored back to originals
            // sort with name change => names changed => restore => keep sorted, but revert names to originals
            // rename last sort => names changed => restore => restore names to originals
            return ComputeAndExecuteRenaming(worker_RenameUndoer, true, string.Empty, true);
        }

        public override string ToString()
        {
            return _renamingParameters.ToString();
        }
        #endregion

        #endregion
    }
}

