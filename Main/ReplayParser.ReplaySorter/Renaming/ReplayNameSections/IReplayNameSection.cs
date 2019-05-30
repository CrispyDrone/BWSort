using ReplayParser.ReplaySorter.CustomFormat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ReplayParser.ReplaySorter.ReplayRenamer
{
    interface IReplayNameSection
    {
        void GenerateSection();
        string GetSection(string separator = "");
        CustomReplayNameSyntax Type { get; }
    }
}
