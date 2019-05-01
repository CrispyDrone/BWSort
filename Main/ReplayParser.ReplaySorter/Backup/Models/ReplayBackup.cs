using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReplayParser.ReplaySorter.Backup.Models
{
    public class ReplayBackup
    {
        public int Id { get; }
        public string Name { get; set; }
        public string Comment { get; set; }
        public string RootDirectory { get; }
        public DateTime Date { get; }
        public ICollection<ReplayFile> ReplayFiles { get; }
    }
}
