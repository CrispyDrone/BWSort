using ReplayParser.ReplaySorter.Backup.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;
using System.IO;
using System.Collections.ObjectModel;

namespace ReplayParser.ReplaySorter.Backup
{
    //TODO it's quite hard to write this in a generic manner since you need a configuration that will tell you how this entity maps to the database. In EF this happens through the DbModel that the context is aware of.
    // I think a simple manner would be to have the Entity itself be responsible for supplying the SQL that tells how to add, delete, read, update it in the database.
    public class BackupRepository : IRepository<Models.Backup>
    {
        #region private

        #region fields

        private BWContext _context;

        #endregion

        #region methods

        private string GetQuery(string queryName)
        {
            return Backup.SQL.Queries.ResourceManager.GetString(queryName);
        }

        #endregion

        #endregion

        #region public

        #region constructor

        public BackupRepository(BWContext context)
        {
            _context = context;
        }

        #endregion

        #endregion

        #region public

        #region methods

        public IEnumerable<Models.Backup> GetAll()
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Models.Backup> Where(Func<Models.Backup, bool> predicate)
        {
            throw new NotImplementedException();
        }

        public int Create(Models.Backup backup)
        {
            //TODO insert backup
            var connection = _context.Connection;
            using (var createBackup = connection.CreateCommand())
            {
                createBackup.CommandText = GetQuery("InsertBackup");
                var backupName = createBackup.CreateParameter();
                var backupComment = createBackup.CreateParameter();
                var backupRootDirectory = createBackup.CreateParameter();

                backupName.Value = backup.Name;
                backupComment.Value = backup.Comment;
                backupRootDirectory.Value = backup.RootDirectory;

                backupName.ParameterName = "@Name";
                backupComment.ParameterName = "@Comment";
                backupRootDirectory.ParameterName = "@RootDirectory";

                var backupId = (int)createBackup.ExecuteScalar();

                foreach (var replayBackup in backup.ReplayBackups)
                {
                    int replayId = 0;

                    using (var getReplay = connection.CreateCommand())
                    {
                        // check if replay already exist
                        getReplay.CommandText = GetQuery("GetReplayIdByHash");
                        var replayHashParam = getReplay.CreateParameter();
                        replayHashParam.ParameterName = "@Hash";

                        replayId = (int)getReplay.ExecuteScalar();
                    }

                    if (replayId == 0)
                    {
                        // IF FALSE => INSERT REPLAY
                        using (var insertReplay = connection.CreateCommand())
                        {
                            insertReplay.CommandText = GetQuery("InsertReplay");
                            var replayHash = insertReplay.CreateParameter();
                            var replayBytes = insertReplay.CreateParameter();

                            replayHash.ParameterName = "@Hash";
                            replayBytes.ParameterName = "@Bytes";

                            replayHash.Value = replayBackup.Replay.Hash;
                            replayBytes.Value = replayBackup.Replay.Bytes;

                            replayId = (int)insertReplay.ExecuteScalar();
                        }

                    }
                    // LINK REPLAY TO BACKUP
                    using (var addReplays = connection.CreateCommand())
                    {
                        addReplays.CommandText = GetQuery("AddReplayToBackup");
                        var replayFileName = addReplays.CreateParameter();
                        var backupIdParam = addReplays.CreateParameter();
                        var replayIdParam = addReplays.CreateParameter();

                        replayFileName.ParameterName = "@FileName";
                        backupIdParam.ParameterName = "@BackupId";
                        replayIdParam.ParameterName = "@ReplayId";

                        replayFileName.Value = replayBackup.FileName;
                        backupIdParam.Value = backupId;
                        replayIdParam.Value = replayId;

                    }

                    backup.Id = backupId;
                    replayBackup.BackupId = backupId; 
                    replayBackup.ReplayId = replayBackup.Replay.Id = replayId;
                }
            }

            return backup.Id;
        }

        public void Remove(int id)
        {
            var connection = _context.Connection;
            using (var removeBackup = connection.CreateCommand())
            {
                removeBackup.CommandText = GetQuery("RemoveBackupByIdWithReplays");
                var backupId = removeBackup.CreateParameter();

                backupId.Value = id;
                backupId.ParameterName = "@Id";

                removeBackup.ExecuteNonQuery();
            }
        }

        public void RemoveAll()
        {
            var connection = _context.Connection;
            using (var removeAllBackupsAndReplays = connection.CreateCommand())
            {
                removeAllBackupsAndReplays.CommandText = GetQuery("RemoveAllBackupsAndReplays");
                removeAllBackupsAndReplays.ExecuteNonQuery();
            }
        }

        public Models.Backup Get(int id)
        {
            var backup = new Models.Backup();
            var connection = _context.Connection;
            using (var command = connection.CreateCommand())
            {
                command.CommandText = GetQuery("GetBackupById");
                var backupId = command.CreateParameter();
                backupId.ParameterName = "@Id";
                backupId.Value = id;

                var reader = command.ExecuteReader();
                if (reader.Read())
                {
                    backup.Id = (int)reader[0];
                    backup.Name = (string)reader[1];
                    backup.Comment = (string)reader[2];
                    backup.RootDirectory = (string)reader[3];
                    backup.Date = (DateTime)reader[4];
                }
            }
            if (backup.Id == 0)
                return null;

            return backup;
        }

        public Models.Backup GetWithReplays(int id)
        {
            var backup = new Models.Backup();
            var connection = _context.Connection;
            using (var command = connection.CreateCommand())
            {
                command.CommandText = GetQuery("GetBackupByIdWithReplays");
                var backupId = command.CreateParameter();
                backupId.ParameterName = "@Id";
                backupId.Value = id;
                var reader = command.ExecuteReader();

                if (reader.Read())
                {
                    backup.Id = (int)reader[0];
                    backup.Name = (string)reader[1];
                    backup.Comment = (string)reader[2];
                    backup.RootDirectory = (string)reader[3];
                    backup.Date = (DateTime)reader[4];

                    if (reader.NextResult())
                    {
                        while (reader.Read())
                        {
                            var replayId = (int)reader[0];
                            var hash = (string)reader[1];
                            var bytes = (byte[])reader[2];
                            var fileName = (string)reader[3];

                            var replayBackup = new ReplayBackup
                            {
                                BackupId = backup.Id,
                                ReplayId = replayId,
                                FileName = fileName,
                                Backup = backup,
                            };

                            var replay = new Replay
                            {
                                Id = replayId,
                                Hash = hash,
                                Bytes = bytes,
                                ReplayBackups = new Collection<ReplayBackup> { replayBackup }
                            };

                            replayBackup.Replay = replay;

                            backup.ReplayBackups.Add(replayBackup);
                        }
                    }
                }
            }

            if (backup.Id == 0)
                return null;

            return backup;
        }

        public int? GetNumberOfBackedUpReplays(int backupId)
        {
            var connection = _context.Connection;
            using (var getReplayCount = connection.CreateCommand())
            {
                getReplayCount.CommandText = GetQuery("GetReplayCountOfBackup");
                var backupIdParam = getReplayCount.CreateParameter();
                backupIdParam.ParameterName = "@Id";
                backupIdParam.Value = backupId;

                return (int?)getReplayCount.ExecuteScalar();
            }
        }

        #endregion

        #endregion
    }
}
