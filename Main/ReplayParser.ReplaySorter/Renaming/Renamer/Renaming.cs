using ReplayParser.Interfaces;
using ReplayParser.ReplaySorter.IO;

namespace ReplayParser.ReplaySorter.ReplayRenamer
{
    public class Renaming
    {
        public Renaming(File<IReplay> replay, string oldName, string newName)
        {
            Replay = replay;
            OldName = oldName;
            NewName = newName;
        }

        public File<IReplay> Replay { get; }
        public string OldName { get; }
        public string NewName { get; }
    }
}
