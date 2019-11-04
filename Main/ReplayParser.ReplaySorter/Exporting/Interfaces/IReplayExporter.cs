namespace ReplayParser.ReplaySorter.Exporting.Interfaces
{
    public interface IReplayExporter
    {
        ServiceResult<ServiceResultSummary> ExportReplays(IExportStrategy exportStrategy, string path);
    }
}
