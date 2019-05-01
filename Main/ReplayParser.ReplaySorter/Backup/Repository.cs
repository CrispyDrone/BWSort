using ReplayParser.ReplaySorter.Backup.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReplayParser.ReplaySorter.Backup
{
    //TODO it's quite hard to write this in a generic manner since you need a configuration that will tell you how this entity maps to the database. In EF this happens through the DbModel that the context is aware of.
    // I think a simple manner would be to have the Entity itself be responsible for supplying the SQL that tells how to add, delete, read, update it in the database.
    public class Repository<T> where T : IEntity
    {
        private BWContext _context;

        public Repository(BWContext context)
        {
            _context = context;
        }

        public IEnumerable<T> GetAll(/*Func<T, string> includedProperties*/)
        {
            // OpenConnection();

            // var command = _context.Connection.CreateCommand();
            // command.CommandText = $"select * from {}";
            throw new NotImplementedException();
        }

        public IEnumerable<T> Where(Func<T, bool> predicate)
        {
            throw new NotImplementedException();
        }

        public void Add(T entity)
        {
            throw new NotImplementedException();
        }

        public void Remove(T entity)
        {
            Remove(entity.Id);
        }

        public void Remove(int id)
        {
            OpenConnection();

            // var command = _context.Connection.CreateCommand();
            // command.CommandText = "delete"
        }

        public T Get(int id)
        {
            throw new NotImplementedException();
        }

        private void OpenConnection()
        {
            if (_context.Connection.State == System.Data.ConnectionState.Closed)
                _context.Connection.Open();
        }
    }
}
