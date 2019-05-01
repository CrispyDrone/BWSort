using ReplayParser.ReplaySorter.Backup.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;
using System.IO;

namespace ReplayParser.ReplaySorter.Backup
{
    public class BWContext : IDisposable
    {
        private string _connectionString;
        private static string CONNECTIONSTRINGFORMAT = "data source={0};Version=3";
        private Repository<ReplayBackup> _backupRepository;
        private SQLiteConnection _connection;
        private SQLiteTransaction _transaction;

        private BWContext(string databaseName)
        {
            if (!(File.Exists(databaseName) || File.Exists(databaseName + ".sqlite")))
            {
                if (Path.GetExtension(databaseName) == string.Empty)
                {
                    databaseName = databaseName + ".sqlite";
                    _connectionString = string.Format(CONNECTIONSTRINGFORMAT, databaseName);
                }

                SQLiteConnection.CreateFile(databaseName);
                _connection = new SQLiteConnection(_connectionString);
            }
        }

        public BWContext Create(string databaseName)
        {
            if (string.IsNullOrWhiteSpace(databaseName)) throw new ArgumentException(nameof(databaseName));

            return new BWContext(databaseName);
        }

        public Repository<ReplayBackup> Backups => _backupRepository ?? (_backupRepository = new Repository<ReplayBackup>(this));
        public SQLiteTransaction CurrentTransaction => _transaction;
        public bool HasActiveTransaction => _transaction != null;
        public SQLiteConnection Connection => _connection;

        public SQLiteTransaction BeginTransaction(/*Isolation Level*/)
        {
            if (_transaction == null)
            {
                if (_connection == null)
                    _connection = new SQLiteConnection(_connectionString);

                if (_connection.State == System.Data.ConnectionState.Closed)
                {
                    _connection.Open();
                }

                _transaction = _connection.BeginTransaction();
            }

            return _transaction;
        }

        public void Commit()
        {
            _transaction?.Commit();
        }

        public void Rollback()
        {
            _transaction?.Rollback();
        }

        public void Dispose()
        {
            _transaction?.Rollback();
            _connection?.Dispose();
        }
    }
}
