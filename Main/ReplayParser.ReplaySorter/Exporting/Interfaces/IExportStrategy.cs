using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ReplayParser.ReplaySorter.Exporting.Interfaces
{
    public interface IExportStrategy
    {
        Task<ServiceResult<ServiceResultSummary>> ExecuteAsync(Stream output, IProgress<int> progress = null);
        Task<ServiceResult<ServiceResultSummary>> ExecuteAsync(Stream output, CancellationToken cancellationToken, IProgress<int> progress = null);
        string Name { get; }
    }
}
