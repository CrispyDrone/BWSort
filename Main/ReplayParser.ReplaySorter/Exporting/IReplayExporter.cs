using ReplayParser.Interfaces;
using ReplayParser.ReplaySorter.IO;
using System.Collections.Generic;

namespace ReplayParser.ReplaySorter.Exporting
{
    public interface IReplayExporter
    {
        ServiceResult<ServiceResultSummary<StringContent>> ExportReplays(IEnumerable<File<IReplay>> replays);
    }
}
