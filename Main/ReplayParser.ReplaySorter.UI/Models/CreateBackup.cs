using ReplayParser.ReplaySorter.Diagnostics;
using ReplayParser.ReplaySorter.IO;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReplayParser.ReplaySorter.UI.Models
{
    public class CreateBackup
    {
        #region private

        #region constructor

        private CreateBackup(string name, string comment, string rootDirectory, IEnumerable<string> files)
        {
            Name = name;
            Comment = comment;
            RootDirectory = rootDirectory;
            Replays = files;
        }

        #endregion

        #endregion

        #region public

        #region static methods

        public static CreateBackup Create(string name, string comment, string rootDirectory, IEnumerable<string> replayFiles)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException(nameof(name));
            if (string.IsNullOrWhiteSpace(comment)) throw new ArgumentException(nameof(comment));
            if (replayFiles == null || replayFiles.Count() == 0) throw new ArgumentException(nameof(replayFiles));

            return new CreateBackup(name, comment, rootDirectory, replayFiles);
        }

        public ReplayParser.ReplaySorter.Backup.Models.Backup ToBackup()
        {
            var backup = new ReplayParser.ReplaySorter.Backup.Models.Backup();
            backup.Name = Name;
            backup.Comment = Comment;
            backup.RootDirectory = RootDirectory;
            var rootDirectory = string.Empty;
            if (RootDirectory.Last() != Path.DirectorySeparatorChar || RootDirectory.Last() != Path.AltDirectorySeparatorChar)
                rootDirectory = RootDirectory + Path.DirectorySeparatorChar;

            ICollection<ReplayParser.ReplaySorter.Backup.Models.ReplayBackup> replayBackups = new Collection<ReplayParser.ReplaySorter.Backup.Models.ReplayBackup>();
            var replayDictionary = new Dictionary<string, ReplayParser.ReplaySorter.Backup.Models.Replay>();

            foreach (var file in Replays)
            {
                try
                {
                    byte[] bytes = System.IO.File.ReadAllBytes(file);
                    string hash = FileHasher.GetMd5Hash(bytes);

                    ReplayParser.ReplaySorter.Backup.Models.Replay replay = null;

                    if (!replayDictionary.TryGetValue(hash, out replay))
                    {
                        replay = new ReplayParser.ReplaySorter.Backup.Models.Replay
                        {
                            Hash = hash,
                            Bytes = bytes
                        };

                        replayDictionary.Add(hash, replay);
                    }

                    var replayBackup = new ReplayParser.ReplaySorter.Backup.Models.ReplayBackup
                    {
                        Backup = backup,
                        FileName = file.Contains(rootDirectory) ? file.Substring(rootDirectory.Length) : file,
                        //FileName = file,
                        Replay = replay
                    };

                    if (replay.ReplayBackups == null)
                        replay.ReplayBackups = new Collection<ReplayParser.ReplaySorter.Backup.Models.ReplayBackup>();

                    replay.ReplayBackups.Add(replayBackup);
                    replayBackups.Add(replayBackup);
                }
                catch (Exception ex)
                {
                    //TODO If a backup can't backup everything, it should fail instead...
                    ErrorLogger.GetInstance()?.LogError($"{DateTime.Now} - Something went wrong while opening or hashing the file", ex: ex);
                }
            }

            backup.ReplayBackups = replayBackups;
            return backup;
        }

        #endregion

        #region properties

        public string Name { get; }
        public string Comment { get; }
        public string RootDirectory { get; }
        public IEnumerable<string> Replays { get; }

        #endregion

        #endregion
    }
}
