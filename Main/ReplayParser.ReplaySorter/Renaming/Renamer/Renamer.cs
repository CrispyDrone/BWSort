using ReplayParser.Interfaces;
using ReplayParser.ReplaySorter.Diagnostics;
using ReplayParser.ReplaySorter.IO;
using ReplayParser.ReplaySorter.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using ReplayParser.ReplaySorter.Renaming;

namespace ReplayParser.ReplaySorter.ReplayRenamer
{
    //TODO refactor this trash...
    public class Renamer
    {
        #region private

        #region fields

        private RenamingParameters _renamingParameters;
        private IEnumerable<File<IReplay>> _listReplays;

        #endregion

        #region methods

        private ServiceResult<ServiceResultSummary<IEnumerable<File<IReplay>>>> ComputeNames(BackgroundWorker worker_ReplayRenamer, string outputDirectory, bool restore = false)
        {
            worker_ReplayRenamer.ReportProgress(0, "Computing names...");

            var firstReplay = _listReplays.FirstOrDefault();
            if (firstReplay == null)
            {
                return new ServiceResult<ServiceResultSummary<IEnumerable<File<IReplay>>>>(ServiceResultSummary<IEnumerable<File<IReplay>>>.Default, false, new List<string> { "You have to parse replays before you can sort. Replay list is empty!" });
            }

            if (!string.IsNullOrWhiteSpace(outputDirectory) && !Directory.Exists(outputDirectory))
            {
                return new ServiceResult<ServiceResultSummary<IEnumerable<File<IReplay>>>>(ServiceResultSummary<IEnumerable<File<IReplay>>>.Default, false, new List<string> { "Output directory does not exist." });
            }

            Stopwatch sw = new Stopwatch();
            sw.Start();
            int currentPosition = 0;
            int progressPercentage = 0;
            int replaysThrowingExceptions = 0;
            var renamedReplays = new List<File<IReplay>>();

            foreach (var replay in _listReplays)
            {
                if (worker_ReplayRenamer.CancellationPending == true)
                {
                    sw.Stop();
                    return new ServiceResult<ServiceResultSummary<IEnumerable<File<IReplay>>>>(
                        new ServiceResultSummary<IEnumerable<File<IReplay>>>(
                            renamedReplays, 
                            $"Renaming cancelled by user... It took {sw.Elapsed} to rename {renamedReplays.Count()} of {_listReplays.Count()} replays. {replaysThrowingExceptions} replays encountered exceptions.", 
                            sw.Elapsed, 
                            currentPosition, 
                            replaysThrowingExceptions
                            ), 
                        true, 
                        null
                    );
                }

                currentPosition++;
                progressPercentage = Convert.ToInt32(((double)currentPosition / (_listReplays.Count() * 2)) * 100);
                worker_ReplayRenamer.ReportProgress(progressPercentage);
                try
                {
                    replay.AddAfterCurrent(
                        (string.IsNullOrWhiteSpace(outputDirectory) ? Directory.GetParent(replay.FilePath).ToString() : outputDirectory ) + @"\" 
                        + 
                        (restore ?  FileHandler.GetFileName(replay.OriginalFilePath) : ReplayHandler.GenerateReplayName(replay.Content, CustomReplayFormat) + ".rep")
                    );
                    renamedReplays.Add(replay);
                }
                catch (Exception ex)
                {
                    replaysThrowingExceptions++;
                    //TODO add to serviceresult instead... log later?
                    ErrorLogger.GetInstance()?.LogError($"{DateTime.Now} - Error while renaming replay: {replay.OriginalFilePath}", ex: ex);
                }
            }

            sw.Stop();
            return new ServiceResult<ServiceResultSummary<IEnumerable<File<IReplay>>>>(
                    new ServiceResultSummary<IEnumerable<File<IReplay>>>(
                        renamedReplays, 
                        $"Finished renaming replays! It took {sw.Elapsed} to rename {_listReplays.Count()} replays. {replaysThrowingExceptions} replays encountered exceptions.",
                        sw.Elapsed,
                        currentPosition,
                        replaysThrowingExceptions
                    ),
                    true, 
                    null
                );
        }

        private ServiceResult<ServiceResultSummary<IEnumerable<File<IReplay>>>> ExecuteRenaming(BackgroundWorker worker_ReplayRenamer, IEnumerable<File<IReplay>> replays, bool shouldCopy, bool forward = true, int steps = 2)
        {
            worker_ReplayRenamer.ReportProgress(50, "Writing replays...");
            // report progress... with "Copying replays with newly generated names..."
            Stopwatch sw = new Stopwatch();
            sw.Start();

            int currentPosition = 0;
            int progressPercentage = 0;
            int replaysThrowingExceptions = 0;
            var renamedReplays = new List<File<IReplay>>();

            foreach (var replay in replays)
            {
                if (worker_ReplayRenamer.CancellationPending == true)
                {
                    sw.Stop();
                    return new ServiceResult<ServiceResultSummary<IEnumerable<File<IReplay>>>>
                        (
                            new ServiceResultSummary<IEnumerable<File<IReplay>>>
                            (
                                renamedReplays, 
                                $"Renaming cancelled by user... It took {sw.Elapsed} to write {renamedReplays.Count()} of {replays.Count()} replays. {replaysThrowingExceptions} replays encountered exceptions.", 
                                sw.Elapsed, 
                                currentPosition, 
                                replaysThrowingExceptions
                            ), 
                            true, 
                            null
                        );
                }

                currentPosition++;
                progressPercentage = 50 + Convert.ToInt32(((double)currentPosition / (replays.Count() * steps)) * 100);
                worker_ReplayRenamer.ReportProgress(progressPercentage);
                try
                {
                    //TODO identical names => failed to create file (add incrementor)
                    replay.SaveState();
                    if (shouldCopy)
                    {
                        ReplayHandler.CopyReplay(replay, forward);
                        // don't want this to show up in history
                        //TODO eh this doesn't take into account forward, but i guess copying only occurs when renaming to an output directory which is always forward??

                        // support for listview renaming action result requires history to be recorded...
                        // replay.Rewind();
                        // replay.RemoveAfterCurrent();

                        // File.Copy(replay.OriginalFilePath, replay.FilePath);
                    }
                    else
                    {
                        ReplayHandler.MoveReplay(replay, forward);
                        // File.Move(replay.OriginalFilePath, replay.FilePath);
                    }
                    renamedReplays.Add(replay);
                    replay.DiscardSavedState();
                }
                catch (Exception ex)
                {
                    replay.RestoreToSavedState();
                    replaysThrowingExceptions++;
                    //TODO add to serviceresult instead... log later?
                    ErrorLogger.GetInstance()?.LogError($"{DateTime.Now} - Error while renaming replay: {replay.OriginalFilePath}", ex: ex);
                }
            }

            sw.Stop();
            return new ServiceResult<ServiceResultSummary<IEnumerable<File<IReplay>>>>
                (
                    new ServiceResultSummary<IEnumerable<File<IReplay>>>
                    (
                        renamedReplays,
                        $"Finished writing replays! It took {sw.Elapsed} to write {replays.Count()} replays. {replaysThrowingExceptions} replays encountered exceptions.",
                        sw.Elapsed,
                        currentPosition,
                        replaysThrowingExceptions
                    ),
                    true,
                    null
                );

        }

        private ServiceResult<ServiceResultSummary<IEnumerable<File<IReplay>>>> ComputeAndExecuteRenaming(BackgroundWorker worker_ReplayRenamer, string outputDirectory, bool restore = false)
        {
            var computationResponse = ComputeNames(worker_ReplayRenamer, OutputDirectory, restore);

            if (!computationResponse.Success)
            {
                return new ServiceResult<ServiceResultSummary<IEnumerable<File<IReplay>>>>(ServiceResultSummary<IEnumerable<File<IReplay>>>.Default, false, new List<string>(computationResponse.Errors));
            }

            var executionResponse = ExecuteRenaming(worker_ReplayRenamer, computationResponse.Result.Result, !string.IsNullOrWhiteSpace(outputDirectory));

            if (!executionResponse.Success)
            {
                return new ServiceResult<ServiceResultSummary<IEnumerable<File<IReplay>>>>(ServiceResultSummary<IEnumerable<File<IReplay>>>.Default, false, new List<string>(executionResponse.Errors));
            }


            // combine both responses...
            //TODO extract to method
            var combinedDuration = computationResponse.Result.Duration + executionResponse.Result.Duration;
            var combinedErrorCount = computationResponse.Result.ErrorCount + executionResponse.Result.ErrorCount;
            var combinedErrors = computationResponse.Errors == null ? 
                (
                    executionResponse.Errors == null ? Enumerable.Empty<string>() : executionResponse.Errors
                ) : 
                (
                    executionResponse.Errors == null ? computationResponse.Errors : computationResponse.Errors.Concat(executionResponse.Errors)
                );
            // union of both replay sets => wrong you only need those replays that have successfully passed both steps
            // IEnumerable<File<IReplay>> combinedReplays = computationResponse.Result.Result.Union(executionResponse.Result.Result);


            return new ServiceResult<ServiceResultSummary<IEnumerable<File<IReplay>>>>
                (
                    new ServiceResultSummary<IEnumerable<File<IReplay>>>
                    (
                        executionResponse.Result.Result,
                        $"Finished renaming replays! It took {combinedDuration} to rename and write {executionResponse.Result.Result.Count()} replays. {combinedErrorCount} exceptions occurred.",
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

        public Renamer(RenamingParameters renamingParameters, IEnumerable<File<IReplay>> listReplays)
        {
            _renamingParameters = renamingParameters;
            _listReplays = listReplays;
        }

        #endregion

        #region properties

        public bool RenameInPlace => _renamingParameters.RenameInPlace;
        public bool RestoreOriginalReplayNames => _renamingParameters.RestoreOriginalReplayNames;
        public string OutputDirectory => _renamingParameters.OutputDirectory;
        public CustomReplayFormat CustomReplayFormat => _renamingParameters.CustomReplayFormat;
        public IEnumerable<File<IReplay>> Replays => _listReplays;

        #endregion

        #region methods

        // how to avoid coupling yourself to backgroundworker...
        // rethink design of not passing parameters, but having them be properties of this renamer class...
        public ServiceResult<ServiceResultSummary<IEnumerable<File<IReplay>>>> RenameInPlaceAsync(BackgroundWorker worker_ReplayRenamer)
        {
            return ComputeAndExecuteRenaming(worker_ReplayRenamer, string.Empty);
        }

        // how to avoid coupling yourself to backgroundworker...
        // rethink design of not passing parameters, but having them be properties of this renamer class...
        public ServiceResult<ServiceResultSummary<IEnumerable<File<IReplay>>>> RenameToDirectoryAsync(BackgroundWorker worker_ReplayRenamer)
        {
            return ComputeAndExecuteRenaming(worker_ReplayRenamer, OutputDirectory);
        }

        public ServiceResult<ServiceResultSummary<IEnumerable<File<IReplay>>>> RestoreOriginalNames(BackgroundWorker worker_RenameUndoer)
        {
            // rename in place => names changed => restore => names restored back to originals
            // sort with name change => names changed => restore => keep sorted, but revert names to originals
            // rename last sort => names changed => restore => restore names to originals
            return ComputeAndExecuteRenaming(worker_RenameUndoer, string.Empty, true);
        }

        public ServiceResult<ServiceResultSummary<IEnumerable<File<IReplay>>>> UndoRename(BackgroundWorker worker_RenameUndoer)
        {
            return ExecuteRenaming(worker_RenameUndoer, _listReplays, false, false, 1);
        }

        public ServiceResult<ServiceResultSummary<IEnumerable<File<IReplay>>>> RedoRename(BackgroundWorker worker_RenameUndoer)
        {
            return ExecuteRenaming(worker_RenameUndoer, _listReplays, false, true, 1);
        }

        public override string ToString()
        {
            return _renamingParameters.ToString();
        }
        #endregion

        #endregion
    }
}

