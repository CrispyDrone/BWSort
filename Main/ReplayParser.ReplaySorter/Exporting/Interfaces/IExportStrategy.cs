using ReplayParser.ReplaySorter.IO;

namespace ReplayParser.ReplaySorter.Exporting.Interfaces
{
    public interface IExportStrategy
    {
        ServiceResult<ServiceResultSummary<StringContent>> Execute();
        string Name { get; }
    }
}
