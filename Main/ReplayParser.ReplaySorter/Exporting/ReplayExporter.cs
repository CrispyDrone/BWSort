using ReplayParser.ReplaySorter.Diagnostics;
using ReplayParser.ReplaySorter.Exporting.Interfaces;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System;

namespace ReplayParser.ReplaySorter.Exporting
{
    public class ReplayExporter : IReplayExporter
    {
        public ServiceResult<ServiceResultSummary> ExportReplays(IExportStrategy exportStrategy, string path)
        {
            var exportResult = exportStrategy.Execute();
            var summary = exportResult.Result;
            var errors = new List<string>(exportResult.Errors);

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var csvFile = exportResult.Result.Result.Content;
            try
            {
                File.WriteAllText(path, csvFile);
            }
            catch (Exception ex)
            {
                errors.Insert(0, ex.Message);
                ErrorLogger.GetInstance()?.LogError($"{DateTime.Now} - Failed to write to file {path}.", ex: ex);
            }

            stopwatch.Stop();

            var totalDuration = exportResult.Result.Duration + stopwatch.Elapsed;

            return new ServiceResult<ServiceResultSummary>(
                new ServiceResultSummary(
                    csvFile,
                    $"It took {totalDuration.TotalSeconds} seconds to export {summary.OperationCount} replays to {exportStrategy.Name} file {path}. " +
                    $"{summary.ErrorCount} replays experienced errors.",
                    totalDuration,
                    summary.OperationCount,
                    summary.ErrorCount
                ), 
                true, 
                errors
            );
        }
    }
}
