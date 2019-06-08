using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReplayParser.ReplaySorter.Backup.Models
{
    public class Replay
    {
        public long Id { get; set; }
        public string Hash { get; set; }
        public byte[] Bytes { get; set; }
        public ICollection<ReplayBackup> ReplayBackups { get; set; }
    }
}
