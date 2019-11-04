using ReplayParser.ReplaySorter.Exporting.Interfaces;
using System.Diagnostics;

namespace ReplayParser.ReplaySorter.Exporting
{
    // use CsvHelper
    public class ReplayExporter : IReplayExporter
    {
        public ServiceResult<ServiceResultSummary> ExportReplays(IExportStrategy exportStrategy, string path)
        {
            var exportResult = exportStrategy.Execute();

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var csvFile = exportResult.Result.Result.Content;
            System.IO.File.WriteAllText(path, csvFile);

            stopwatch.Stop();

            var totalDuration = exportResult.Result.Duration + stopwatch.Elapsed;

            return new ServiceResult<ServiceResultSummary>(
                new ServiceResultSummary(
                    csvFile,
                    $"It took {totalDuration.TotalSeconds} seconds to export {exportResult.Result.OperationCount} replays to {exportStrategy.Name} file {path}. {exportResult.Result.ErrorCount} replays experienced errors.",
                    totalDuration,
                    exportResult.Result.OperationCount,
                    exportResult.Result.ErrorCount
                ), 
                true, 
                null
            );
        }
    }
}
