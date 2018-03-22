using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ReplayParser.ReplaySorter.Sorting.SortCommands
{
    public class SortCommandFactory
    {
        public ISortCommand GetSortCommand(Criteria sortcriteria, SortCriteriaParameters sortcriteriaparameters, bool keeporiginalreplaynames, Sorter sorter)
        {
            // how to write this properly? 
            if (sortcriteria.HasFlag(Criteria.PLAYERNAME))
            {
                return new SortOnPlayerName(sortcriteriaparameters, keeporiginalreplaynames, sorter);
            }
            else if (sortcriteria.HasFlag(Criteria.GAMETYPE))
            {
                return new SortOnGameType(sortcriteriaparameters, keeporiginalreplaynames, sorter);
            }
            else if (sortcriteria.HasFlag(Criteria.MATCHUP))
            {
                return new SortOnMatchUp(sortcriteriaparameters, keeporiginalreplaynames, sorter);
            }
            else if (sortcriteria.HasFlag(Criteria.MAP))
            {
                return new SortOnMap(sortcriteriaparameters, keeporiginalreplaynames, sorter);
            }
            else if (sortcriteria.HasFlag(Criteria.DURATION))
            {
                return new SortOnDuration(sortcriteriaparameters, keeporiginalreplaynames, sorter);
            }
            else
            {
                return null;
            }
        }
    }
}
