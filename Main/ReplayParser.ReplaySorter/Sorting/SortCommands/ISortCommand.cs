using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ReplayParser.ReplaySorter.Sorting;
using ReplayParser.Interfaces;

namespace ReplayParser.ReplaySorter.Sorting.SortCommands
{
    public interface ISortCommand
    {
        IDictionary<string, IDictionary<string,IReplay>> Sort();

        SortCriteriaParameters SortCriteriaParameters { get; set; }

        Criteria SortCriteria { get; }

        bool KeepOriginalReplayNames { get; set; }

        Sorter Sorter { get; set; }

        bool IsNested { get; set; }
    }
}
