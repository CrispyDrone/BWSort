using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReplayParser.ReplaySorter.Backup
{
    public interface IRepository<T>
    {
        IEnumerable<T> GetAll();
        IEnumerable<T> Where(Func<T, bool> predicate);
        int Create(T entity);
        void Remove(int id);
        T Get(int id);
    }
}
