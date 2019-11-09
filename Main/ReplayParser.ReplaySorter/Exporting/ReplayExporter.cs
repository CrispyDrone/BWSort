using ReplayParser.ReplaySorter.Diagnostics;
using ReplayParser.ReplaySorter.Exporting.Interfaces;
using ReplayParser.Interfaces;
using ReplayParser.ReplaySorter.Exporting.Strategies;
using ReplayParser.ReplaySorter.IO;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace ReplayParser.ReplaySorter.Exporting
{
    public class ReplayExporter : IReplayExporter
    {
        public ReplayExporter(IEnumerable<File<IReplay>> replays)
        {
            Replays = replays;
        }

        public IEnumerable<File<IReplay>> Replays { get; }

        public async Task<ServiceResult<ServiceResultSummary>> ExportToCsvAsync(
            string path, 
            ICsvConfiguration csvConfiguration, 
            IProgress<int> progress = null
        )
        {
            return await ExportToCsvAsync(path, csvConfiguration, CancellationToken.None, progress);
        }

        public async Task<ServiceResult<ServiceResultSummary>> ExportToCsvAsync(
            string path, 
            ICsvConfiguration csvConfiguration, 
            CancellationToken cancellationToken, 
            IProgress<int> progress = null
        )
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var errors = new List<string>();
            var exportStrategy = new CsvExportStrategy(Replays, csvConfiguration);
            ServiceResult<ServiceResultSummary> exportResult = null;

            try
            {
                using (var fs = File.OpenWrite(path))
                {
                    exportResult = await exportStrategy.ExecuteAsync(fs, cancellationToken, progress);
                }
            }
            catch (Exception ex)
            {
                ErrorLogger.GetInstance()?.LogError($"{DateTime.Now} - Failed to write to stream.", ex: ex);
                errors.Add(ex.Message);

                stopwatch.Stop();
                return new ServiceResult<ServiceResultSummary>(
                    new ServiceResultSummary(
                        null,
                        "Something went wrong while exporting.",
                        stopwatch.Elapsed,
                        Replays.Count(),
                        1
                     ),
                    false,
                    errors
                 );
            }

            var summary = exportResult.Result;
            errors.AddRange(exportResult.Errors);
            stopwatch.Stop();
            var totalDuration = exportResult.Result.Duration + stopwatch.Elapsed;

            return new ServiceResult<ServiceResultSummary>(
                new ServiceResultSummary(
                    null,
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
