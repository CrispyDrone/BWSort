using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ReplayParser.Interfaces;

namespace ReplayParser.ReplaySorter.ReplayRenamer
{
    class Date : IReplayNameSection
    {
        public Date(IReplay areplay)
        {
            Replay = areplay;
            GenerateSection();
        }

        public IReplay Replay { get; set; }

        public string Value { get; set; }

        public CustomReplayNameSyntax Type { get { return CustomReplayNameSyntax.D; } }

        public void GenerateSection()
        {
            string Date = Replay.Timestamp.ToShortDateString();
            Value = Date.Replace('/', '-');
        }

        public string GetSection(string separator)
        {
            return Value;
        }
    }
}
