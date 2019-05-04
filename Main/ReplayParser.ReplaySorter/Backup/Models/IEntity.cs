using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReplayParser.ReplaySorter.Backup.Models
{
    public interface IEntity
    {
        string CreateQueryFormat { get; }
        string GetQueryFormat { get; }
        string GetAllQueryFormat { get; }
        string UpdateQueryFormat { get; }
        string RemoveQueryFormat { get; }
        IEntity Include(IEntity entity);
    }
}
