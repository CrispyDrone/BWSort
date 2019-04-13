using System;
using System.IO;
using System.Text.RegularExpressions;

namespace ReplayParser.ReplaySorter.Configuration
{
    public class ReplaySorterAppConfiguration : IReplaySorterConfiguration
    {
        #region private

        #region fields

        private string _logDirectory;
        private uint _maxUndoLevel;
        private bool _checkForUpdates;
        private bool _rememberParsingDirectory;
        private string _lastParsingDirectory;
        private bool _includeSubDirectoriesByDefault;
        private bool _loadReplaysOnStartup;
        private bool _checkForDuplicatesOnCumulativeParsing;

        private bool _logDirectoryChanged = true;
        private bool _maxUndoLevelChanged = true;
        private bool _checkForUpdatesChanged = true;
        private bool _rememberParsingDirectoryChanged = true;
        private bool _lastParsingDirectoryChanged = true;
        private bool _includeSubDirectoriesByDefaultChanged = true;
        private bool _loadReplaysOnStartupChanged = true;
        private bool _checkForDuplicatesOnCumulativeParsingChanged = true;

        #endregion

        #region methods
        private void Save()
        {
            Properties.Settings.Default.Save();
        }
        #endregion

        #endregion

        #region public

        #region properties

        public string RepositoryUrl => "https://www.github.com/crispydrone/bwsort";
        public string GithubAPIRepoUrl => "https://api.github.com/repos/crispydrone/bwsort";
        public string Version => "v0.9";
        public Regex VersionRegex => new Regex("\"tag_name\":\\s*\"(.*?)\"", RegexOptions.IgnoreCase);
        public string LogDirectory
        {
            get
            {
                if (_logDirectoryChanged)
                {
                    var logDirectorySetting = Properties.Settings.Default.LOGDIRECTORY;
                    if (string.IsNullOrWhiteSpace(logDirectorySetting))
                    {
                        _logDirectory = AppDomain.CurrentDomain.BaseDirectory;
                    }
                    else
                    {
                        _logDirectory = logDirectorySetting;
                    }

                    _logDirectoryChanged = false;
                }
                return _logDirectory;
            }
            set
            {
                if (string.IsNullOrWhiteSpace(value) || !Directory.Exists(value))
                    return;

                _logDirectoryChanged = true;
                Properties.Settings.Default.LOGDIRECTORY = value;
                Save();
            }
        }

        public uint MaxUndoLevel
        {
            get
            {
                if (_maxUndoLevelChanged)
                {
                    _maxUndoLevel = Properties.Settings.Default.MAXUNDOLEVEL;
                    _maxUndoLevelChanged = false;
                }
                return _maxUndoLevel;
            }
            set
            {
                if (value > 0)
                {
                    _maxUndoLevelChanged = true;
                    Properties.Settings.Default.MAXUNDOLEVEL = value;
                    Save();
                }
            }
        }

        public bool CheckForUpdates
        {
            get
            {
                if (_checkForUpdatesChanged)
                {
                    _checkForUpdates = Properties.Settings.Default.CHECKFORUPDATES;
                    _checkForUpdatesChanged = false;
                }
                return _checkForUpdates;
            }
            set
            {
                _checkForUpdatesChanged = true;
                Properties.Settings.Default.CHECKFORUPDATES = value;
                Save();
            }
        }

        public bool RememberParsingDirectory
        {
            get
            {
                if (_rememberParsingDirectoryChanged)
                {
                    _rememberParsingDirectory = Properties.Settings.Default.REMEMBERPARSINGDIRECTORY;
                    _rememberParsingDirectoryChanged = false;
                }
                return _rememberParsingDirectory;
            }
            set
            {
                _rememberParsingDirectoryChanged = true;
                Properties.Settings.Default.REMEMBERPARSINGDIRECTORY = value;
                Save();
            }
        }

        public string LastParsingDirectory
        {
            get
            {
                if (_lastParsingDirectoryChanged)
                {
                    _lastParsingDirectory = Properties.Settings.Default.LASTPARSINGDIRECTORY;
                    _lastParsingDirectoryChanged = false;
                }
                return _lastParsingDirectory;
            }
            set
            {
                if (string.IsNullOrWhiteSpace(value) || !Directory.Exists(value))
                    return;

                _lastParsingDirectoryChanged = true;
                Properties.Settings.Default.LASTPARSINGDIRECTORY = value;
                Save();
            }
        }

        public bool IncludeSubDirectoriesByDefault
        {
            get
            {
                if (_includeSubDirectoriesByDefaultChanged)
                {
                    _includeSubDirectoriesByDefault = Properties.Settings.Default.PARSESUBDIRECTORIES;
                    _includeSubDirectoriesByDefaultChanged = false;
                }

                return _includeSubDirectoriesByDefault;
            }
            set
            {
                _includeSubDirectoriesByDefaultChanged = true;
                Properties.Settings.Default.PARSESUBDIRECTORIES = value;
                Save();
            }
        }

        public bool LoadReplaysOnStartup
        {
            get
            {
                if (_loadReplaysOnStartupChanged)
                {
                    _loadReplaysOnStartup = Properties.Settings.Default.LOADREPLAYSONSTARTUP;
                    _loadReplaysOnStartupChanged = false;
                }

                return _loadReplaysOnStartup;
            }
            set
            {
                _loadReplaysOnStartupChanged = true;
                Properties.Settings.Default.LOADREPLAYSONSTARTUP = value;
                Save();
            }
        }

        public bool CheckForDuplicatesOnCumulativeParsing
        {
            get
            {
                if (_checkForDuplicatesOnCumulativeParsingChanged)
                {
                    _checkForDuplicatesOnCumulativeParsing = Properties.Settings.Default.CHECKFORDUPLICATES;
                    _checkForDuplicatesOnCumulativeParsingChanged = false;
                }

                return _checkForDuplicatesOnCumulativeParsing;
            }
            set
            {
                _checkForDuplicatesOnCumulativeParsingChanged = true;
                Properties.Settings.Default.CHECKFORDUPLICATES = value;
                Save();
            }
        }

        #endregion

        #endregion
    }
}
