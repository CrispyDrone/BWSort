using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ReplayParser.Interfaces;
using System.IO;

namespace ReplayParser.ReplaySorter.ReplayRenamer
{
    public class Map : IReplayNameSection
    {
        private static char[] InvalidFileChars = Path.GetInvalidPathChars();
        private static char[] InvalidFileCharsAdditional = new char[] { '*' };
        public Map(IReplay areplay)
        {
            Replay = areplay;
            GenerateSection();
        }

        public IReplay Replay { get; set; }

        public string Name { get; set; }

        public CustomReplayNameSyntax Type { get { return CustomReplayNameSyntax.M; } }

        public void GenerateSection()
        {
            string MapName = Replay.ReplayMap.MapName;
            // remove any invalid characters
            foreach (char invalidChar in InvalidFileChars)
            {
                MapName = MapName.Replace(invalidChar.ToString(), "");
            }
            foreach (char invalidChar in InvalidFileCharsAdditional)
            {
                MapName = MapName.Replace(invalidChar.ToString(), "");
            }
            Name = MapName;
        }

        public string GetSection(string separator)
        {
            return Name.Replace('.', ',');
        }
    }
}
