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
            var backups = new List<Models.Backup>();
            var connection = _context.Connection;
            using (var command = connection.CreateCommand())
            {
                command.CommandText = GetQuery("GetAllBackups");
                var backupId = command.CreateParameter();

                var reader = command.ExecuteReader();
                if (reader.Read())
                {
                    var backup = new Models.Backup();
                    backup.Id = (long)reader[0];
                    backup.Name = (string)reader[1];
                    backup.Comment = (string)reader[2];
                    backup.RootDirectory = (string)reader[3];
                    backup.Date = DateTime.Parse(reader[4].ToString());
                    backups.Add(backup);
                }
            }

            return backups.AsEnumerable();
        }

        public IEnumerable<Models.Backup> Where(Func<Models.Backup, bool> predicate)
        {
            throw new NotImplementedException();
        }

        public long Create(Models.Backup backup)
        {
            var connection = _context.Connection;
            using (var createBackup = connection.CreateCommand())
            {
                createBackup.CommandText = GetQuery("InsertBackup");
                createBackup.Parameters.Add(new SQLiteParameter("@Name", backup.Name));
                createBackup.Parameters.Add(new SQLiteParameter("@Comment", backup.Comment));
                createBackup.Parameters.Add(new SQLiteParameter("@RootDirectory", backup.RootDirectory));

                var backupResult = createBackup.ExecuteScalar();
                backupResult = (backupResult == DBNull.Value) ? null : backupResult;
                var backupId = Convert.ToInt64(backupResult);

                foreach (var replayBackup in backup.ReplayBackups)
                {
                    long replayId = 0;

                    using (var getReplay = connection.CreateCommand())
                    {
                        // check if replay already exist
                        getReplay.CommandText = GetQuery("GetReplayIdByHash");
                        getReplay.Parameters.Add(new SQLiteParameter("@Hash", replayBackup.Replay.Hash));

                        var replayResult = getReplay.ExecuteScalar();
                        replayResult = (replayResult == DBNull.Value) ? null : replayResult;
                        replayId = Convert.ToInt64(replayResult);
                    }

                    if (replayId == 0)
                    {
                        // IF FALSE => INSERT REPLAY
                        using (var insertReplay = connection.CreateCommand())
                        {
                            insertReplay.CommandText = GetQuery("InsertReplay");
                            insertReplay.Parameters.Add(new SQLiteParameter("@Hash", replayBackup.Replay.Hash));
                            insertReplay.Parameters.Add(new SQLiteParameter("@Bytes", replayBackup.Replay.Bytes));

                            var replayResult = insertReplay.ExecuteScalar();
                            replayResult = (replayResult == DBNull.Value) ? null : replayResult;
                            if (replayResult == null)
                                throw new Exception($"Failed to insert replay {replayBackup.FileName} into database!");

                            replayId = Convert.ToInt64(replayResult);
                        }
                    }

                    // LINK REPLAY TO BACKUP
                    using (var addReplays = connection.CreateCommand())
                    {
                        addReplays.CommandText = GetQuery("AddReplayToBackup");
                        addReplays.Parameters.Add(new SQLiteParameter("@FileName", replayBackup.FileName));
                        addReplays.Parameters.Add(new SQLiteParameter("@BackupId", backupId));
                        addReplays.Parameters.Add(new SQLiteParameter("@ReplayId", replayId));
                        addReplays.ExecuteNonQuery();
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

        public Models.Backup Get(long id)
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
                    backup.Id = (long)reader[0];
                    backup.Name = (string)reader[1];
                    backup.Comment = (string)reader[2];
                    backup.RootDirectory = (string)reader[3];
                    backup.Date = DateTime.Parse(reader[4].ToString());
                }
            }
            if (backup.Id == 0)
                return null;

            return backup;
        }

        //TODO this doesn't create backup properly (replaybackups can refere to same replay object)
        public Models.Backup GetWithReplays(long id)
        {
            var backup = new Models.Backup();
            var connection = _context.Connection;
            using (var command = connection.CreateCommand())
            {
                command.CommandText = GetQuery("GetBackupByIdWithReplays");
                command.Parameters.Add(new SQLiteParameter("@Id", id));
                var reader = command.ExecuteReader();

                if (reader.Read())
                {
                    backup.Id = (long)reader[0];
                    backup.Name = (string)reader[1];
                    backup.Comment = (string)reader[2];
                    backup.RootDirectory = (string)reader[3];
                    backup.Date = DateTime.Parse(reader[4].ToString());

                    if (reader.NextResult())
                    {
                        while (reader.Read())
                        {
                            var replayId = (long)reader[0];
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

        public int? GetNumberOfBackedUpReplays(long backupId)
        {
            var connection = _context.Connection;
            using (var getReplayCount = connection.CreateCommand())
            {
                getReplayCount.CommandText = GetQuery("GetReplayCountOfBackup");
                getReplayCount.Parameters.Add(new SQLiteParameter("@Id", backupId));
                var countResult = getReplayCount.ExecuteScalar();
                countResult = (countResult == DBNull.Value) ? null : countResult;
                if (countResult == null)
                    return null;

                return Convert.ToInt32(countResult);
            }
        }

        #endregion

        #endregion
    }
}
