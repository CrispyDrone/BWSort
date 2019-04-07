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
        string LogDirectory { get; }
        int MaxUndoLevel { get; }
    }
}
