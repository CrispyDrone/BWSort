using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReplayParser.ReplaySorter.Backup
{
    public class BackupService
    {
        private readonly BWContext _context;

        public BackupService(BWContext context)
        {
            _context = context;
        } 

        public void DeleteAllBackupsAndReplays()
        {
            _context.BackupRepository.RemoveAll();
            _context.Commit();
        }
    }
}
