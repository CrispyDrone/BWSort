using ReplayParser.ReplaySorter.IO;
using System.IO;
using System.Threading.Tasks;

namespace ReplayParser.ReplaySorter.Exporting.Interfaces
{
    public interface IExportStrategy
    {
        Task<ServiceResult<ServiceResultSummary>> ExecuteAsync(Stream output);
        string Name { get; }
    }
}
