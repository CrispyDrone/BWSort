using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace ReplayParser.ReplaySorter.Ignoring
{
    public class IgnoreFileManager
    {
        #region private

        #region fields

        private IFormatter _formatter = new BinaryFormatter();
        private static readonly string _extension = ".repignore";
        private static Dictionary<string, bool> _ignoreFilesChanged = new Dictionary<string, bool>();
        private static Dictionary<string, IgnoreFile> _ignoreFiles = new Dictionary<string, IgnoreFile>();

        #endregion

        #region methods

        private void LoadAndUpdateCache(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException(nameof(path)); 
            if (Path.GetExtension(path).ToLower() != ".repignore") throw new InvalidOperationException("Can only load \".repignore\" files!"); 

            if (!File.Exists(path))
            {
                RemoveFromCache(path);
                throw new FileNotFoundException($"File not found at following location {path}");
            }

            bool isModified;
            if (_ignoreFilesChanged.TryGetValue(path, out isModified))
            {
                if (isModified)
                {
                    using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        AddOrUpdate(path, _ignoreFiles, _formatter.Deserialize(fs) as IgnoreFile);
                    }
                    _ignoreFilesChanged[path] = false;
                }
            }
            else
            {
                using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    AddOrUpdate(path, _ignoreFiles, _formatter.Deserialize(fs) as IgnoreFile);
                }
                _ignoreFilesChanged.Add(path, false);
            }
        }

        private void SaveAndUpdateCache(IgnoreFile ignoreFile, string path, bool overwrite)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException(nameof(path));
            if (ignoreFile == null) throw new ArgumentException(nameof(ignoreFile));
            if (File.Exists(path) && overwrite == false) throw new InvalidOperationException("File already exists and overwrite is not specified as true!");
            if (Path.GetExtension(path).ToLower() != _extension) throw new InvalidOperationException("Extension \".repignore\" has to be specified!");

            using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                _formatter.Serialize(fs, ignoreFile);
            }

            AddOrUpdate(path, _ignoreFiles, ignoreFile);
            AddOrUpdate(path, _ignoreFilesChanged, true);
        }

        private void AddOrUpdate<T>(string key, Dictionary<string, T> dictionary, T value)
        {
            if (dictionary.ContainsKey(key))
            {
                dictionary[key] = value;
            }
            else
            {
                dictionary.Add(key, value);
            }
        }

        private void RemoveFromCache(string path)
        {
            if (_ignoreFiles.ContainsKey(path))
            {
                _ignoreFiles.Remove(path);
            }
            if (_ignoreFilesChanged.ContainsKey(path))
            {
                _ignoreFilesChanged.Remove(path);
            }
        }

        #endregion

        #endregion

        #region public

        #region methods

        public IgnoreFile Load(string path)
        {
            LoadAndUpdateCache(path);
            return _ignoreFiles[path];
        }

        public void Save(IgnoreFile ignoreFile, string path, bool overwrite = false)
        {
            SaveAndUpdateCache(ignoreFile, path, overwrite);
        }

        public void Delete(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException(nameof(path));
            if (Path.GetExtension(path).ToLower() != _extension) throw new InvalidOperationException("Extension \".repignore\" has to be specified!");
            if (!File.Exists(path)) throw new InvalidOperationException("File does not exist!");

            File.Delete(path);
            RemoveFromCache(path);
        }

        #endregion

        #endregion
    }
}
