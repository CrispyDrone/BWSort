using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReplayParser.ReplaySorter.Backup.Models
{
    public class Backup : IEntity
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Comment { get; set; }
        public string RootDirectory { get; }
        public DateTime Date { get; set; }
        public ICollection<Replay> ReplayFiles { get; set; }

        public string CreateQueryFormat => "insert into backups (name, comment, rootdirectory, date) values ({0}, {1}, {2}, {3})";
        public string GetQueryFormat => "select * from backups where id={0}";
        public string GetAllQueryFormat => "select * from backups";
        public string UpdateQueryFormat => "update backups set name={1}, comment={2} where id={0}";
        public string RemoveQueryFormat => "remove from backups where id={0}";
    }
}
