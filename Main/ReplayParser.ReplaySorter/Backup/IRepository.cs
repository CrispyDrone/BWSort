using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReplayParser.ReplaySorter.Backup
{
    public interface IRepository<T>
    {
        long Create(T entity);
        T Get(long id);
        IEnumerable<T> GetAll();
        IEnumerable<T> Where(Func<T, bool> predicate);
        void Remove(long id);
        void RemoveAll();
    }
}
