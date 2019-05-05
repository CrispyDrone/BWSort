using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReplayParser.ReplaySorter.Backup
{
    public interface IRepository<T>
    {
        int Create(T entity);
        T Get(int id);
        IEnumerable<T> GetAll();
        IEnumerable<T> Where(Func<T, bool> predicate);
        void Remove(int id);
        void RemoveAll();
    }
}
