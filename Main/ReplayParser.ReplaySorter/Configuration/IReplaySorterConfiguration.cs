using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ReplayParser.ReplaySorter.Configuration
{
    public interface IReplaySorterConfiguration
    {
        string RepositoryUrl { get; }
        string GithubAPIRepoUrl { get; }
        string Version { get; }
        Regex VersionRegex { get; }
        string LogDirectory { get; set; }
        uint MaxUndoLevel { get; set; }
        bool CheckForUpdates { get; set; }
        bool RememberParsingDirectory { get; set; }
        string LastParsingDirectory { get; set; }
        bool IncludeSubDirectoriesByDefault { get; set; }
        bool LoadReplaysOnStartup { get; set; }
        bool CheckForDuplicatesOnCumulativeParsing { get; set; }
    }
}
