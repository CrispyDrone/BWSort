using System;
using System.Configuration;
using System.Text.RegularExpressions;

namespace ReplayParser.ReplaySorter.Configuration
{
    public class ReplaySorterAppConfiguration : IReplaySorterConfiguration
    {
        public string RepositoryUrl => "https://www.github.com/crispydrone/bwsort";
        public string GithubAPIRepoUrl => "https://api.github.com/repos/crispydrone/bwsort";
        public string Version => "v0.9";
        public Regex VersionRegex => new Regex("\"tag_name\":\\s*\"(.*?)\"");
        public string LogDirectory => ConfigurationManager.AppSettings.Get("LogDirectory") ?? AppDomain.CurrentDomain.BaseDirectory;
        public int MaxUndoLevel
        {
            get
            {
                int maxUndolevel;
                if (!int.TryParse(ConfigurationManager.AppSettings.Get("MaxUndoLevel"), out maxUndolevel))
                    return 10;

                return maxUndolevel;
            }
        }
    }
}
