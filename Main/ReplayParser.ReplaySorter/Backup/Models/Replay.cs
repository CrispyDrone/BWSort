using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReplayParser.ReplaySorter.Backup.Models
{
    public class Replay : IEntity
    {
        public string Hash { get; set; }
        public byte[] Bytes { get; set; }
        public ICollection<Backup> Backups { get; set; }
        // public string FileName { get; }

        public string CreateQueryFormat => "insert into replayfiles (hash, bytes) values ({0}, {1})";
        public string GetQueryFormat => "select * from replayfiles where hash={0}";
        public string GetAllQueryFormat => "select * from replayfiles";
        public string UpdateQueryFormat => string.Empty;//"update replayfiles set hash={0}, bytes={1}";
        public string RemoveQueryFormat => "remove from replayfiles where hash={0}";
    }
}
