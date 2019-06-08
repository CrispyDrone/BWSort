using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReplayParser.ReplaySorter.Backup.Models
{
    public class ReplayBackup
    {
        public long BackupId { get; set; }
        public long ReplayId { get; set; }
        public string FileName { get; set; }

        public Replay Replay { get; set; }
        public Backup Backup { get; set; }
    }
}
