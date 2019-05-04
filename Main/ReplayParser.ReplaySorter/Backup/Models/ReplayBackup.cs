using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReplayParser.ReplaySorter.Backup.Models
{
    public class ReplayBackup : IEntity
    {
        public int BackupId { get; }
        public string Hash { get; }
        public string FileName { get; set; }

        public string CreateQueryFormat => "insert into replaybackups (backupid, hash, filename) values ({0}, {1}, {2})";
        public string GetQueryFormat => "select * from replaybackups where backupid={0} and hash={1}";
        public string GetAllQueryFormat => "select * from replaybackups";
        public string UpdateQueryFormat => "update replaybackups set filename={0} where backupid={0} and hash={1}";
        public string RemoveQueryFormat => "remove from replaybackups where backupid={0} and hash={1}";

        public IEntity Include(IEntity entity)
        {
        }
    }
}
