using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ReplayParser.ReplaySorter.Sorting;
using ReplayParser.Interfaces;
using System.ComponentModel;

namespace ReplayParser.ReplaySorter.Sorting.SortCommands
{
    public interface ISortCommand
    {
        IDictionary<string, List<File<IReplay>>> Sort(List<string> replaysThrowingExceptions);

        IDictionary<string, List<File<IReplay>>> SortAsync(List<string> replaysThrowingExceptions, BackgroundWorker worker_ReplaySorter, int currentCriteria, int numberOfCriteria, int currentPositionNested = 0, int numberOfPositions = 0);

        SortCriteriaParameters SortCriteriaParameters { get; set; }

        Criteria SortCriteria { get; }

        bool KeepOriginalReplayNames { get; set; }

        Sorter Sorter { get; set; }

        bool IsNested { get; set; }
    }
}
