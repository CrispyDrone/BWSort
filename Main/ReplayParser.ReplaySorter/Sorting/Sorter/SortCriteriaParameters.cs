using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ReplayParser.Entities;

namespace ReplayParser.ReplaySorter.Sorting
{
    public class SortCriteriaParameters
    {
        public SortCriteriaParameters(bool? makefolderforwinner = null, bool? makefolderforloser = null, IDictionary<GameType, bool> validgametypes = null, int[] durations = null)
        {
            this.MakeFolderForWinner = makefolderforwinner;
            this.MakeFolderForLoser = makefolderforloser;
            this.ValidGameTypes = validgametypes;
            this.Durations = durations;
        }

        public bool? MakeFolderForWinner { get; set; }

        public bool? MakeFolderForLoser { get; set; }

        public IDictionary<GameType, bool> ValidGameTypes { get; set; }

        public int[] Durations { get; set; }
    }
}
