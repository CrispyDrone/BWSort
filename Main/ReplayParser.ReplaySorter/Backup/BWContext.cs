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
        private BackupRepository _backupRepository;
        private SQLiteConnection _connection;
        private SQLiteTransaction _transaction;

        private BWContext(string databaseName)
        {
            _connectionString = string.Format(CONNECTIONSTRINGFORMAT, databaseName);
        }

        public static BWContext Create(string databaseName, bool createIfNotExist = true)
        {
            if (string.IsNullOrWhiteSpace(databaseName)) throw new ArgumentException(nameof(databaseName));

            if (Path.GetExtension(databaseName) == string.Empty)
                databaseName = databaseName + ".sqlite";

            if (!File.Exists(databaseName))
            {
                if (!createIfNotExist) throw new InvalidOperationException("Database does not exist but CreateIfNotExists is false!");

                SQLiteConnection.CreateFile(databaseName);
                // _connection = new SQLiteConnection(_connectionString);
            }

            return new BWContext(databaseName);
        }

        public BackupRepository Backups => _backupRepository ?? (_backupRepository = new BackupRepository(this));
        public SQLiteTransaction CurrentTransaction => _transaction;
        public bool HasActiveTransaction => _transaction != null;
        public SQLiteConnection Connection
        {
            get
            {
                if (_connection == null)
                    _connection = new SQLiteConnection(_connectionString);

                if (_connection.State == System.Data.ConnectionState.Closed)
                    _connection.Open();

                if (_transaction == null)
                    _transaction = _connection.BeginTransaction();

                return _connection;
            }
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
