using ReplayParser.ReplaySorter.Renaming.Enums;

namespace ReplayParser.ReplaySorter.ReplayRenamer
{
    //TODO remove
    interface IReplayNameSection
    {
        void GenerateSection();
        string GetSection(string separator = "");
        ReplayNameSectionType Type { get; }
    }
}
