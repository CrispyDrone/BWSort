using System.Collections.Generic;
using ReplayParser.Interfaces;
using System.ComponentModel;
using ReplayParser.ReplaySorter.IO;

namespace ReplayParser.ReplaySorter.Sorting.SortCommands
{
    public interface ISortCommand
    {
        IDictionary<string, List<File<IReplay>>> Sort(List<string> replaysThrowingExceptions);

        IDictionary<string, List<File<IReplay>>> SortAsync(List<string> replaysThrowingExceptions, BackgroundWorker worker_ReplaySorter, int currentCriteria, int numberOfCriteria, int currentPositionNested = 0, int numberOfPositions = 0);

        IDictionary<string, List<File<IReplay>>> PreviewSort(List<string> replaysThrowingExceptions, BackgroundWorker worker_ReplaySorter, int currentCriteria, int numberOfCriteria, int currentPositionNested = 0, int numberOfPositions = 0);

        SortCriteriaParameters SortCriteriaParameters { get; set; }

        Criteria SortCriteria { get; }

        bool KeepOriginalReplayNames { get; set; }

        Sorter Sorter { get; set; }

        bool IsNested { get; set; }
    }
}
