using System.IO;
using System.Threading.Tasks;

namespace ReplayParser.ReplaySorter.Exporting.Interfaces
{
    public interface IReplayExporter
    {
        Task<ServiceResult<ServiceResultSummary>> ExportToCsvAsync(string path, ICsvConfiguration csvConfiguration);
    }
}
