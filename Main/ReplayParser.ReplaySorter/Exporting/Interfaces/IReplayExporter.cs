using System.Threading.Tasks;
using System.Threading;
using System;

namespace ReplayParser.ReplaySorter.Exporting.Interfaces
{
    public interface IReplayExporter
    {
        Task<ServiceResult<ServiceResultSummary>> ExportToCsvAsync(string path, ICsvConfiguration csvConfiguration, IProgress<int> progress = null);
        Task<ServiceResult<ServiceResultSummary>> ExportToCsvAsync(string path, ICsvConfiguration csvConfiguration, CancellationToken cancellationToken, IProgress<int> progress = null);
    }
}
