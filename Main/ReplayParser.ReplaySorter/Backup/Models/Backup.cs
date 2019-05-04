using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReplayParser.ReplaySorter.Backup.Models
{
    public class Backup
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Comment { get; set; }
        public string RootDirectory { get; set; }
        public DateTime Date { get; set; }
        public ICollection<ReplayBackup> ReplayBackups { get; set; }
    }
}
