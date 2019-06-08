using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReplayParser.ReplaySorter.UI.Models
{
    public class BackupWithCount
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string Comment { get; set; }
        public string RootDirectory { get; set; }
        public DateTime Date  { get; set; }
        public int Count { get; set; }
    }
}
