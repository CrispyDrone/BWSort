using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ReplayParser.Entities;

namespace ReplayParser.ReplaySorter.Sorting
{
    public class SortCriteriaParameters
    {
        #region private

        #region fields

        private string _cachedStringRepresentation;

        #endregion

        #endregion

        #region public

        #region constructor

        public SortCriteriaParameters(bool? makefolderforwinner = null, bool? makefolderforloser = null, IDictionary<GameType, bool> validgametypes = null, int[] durations = null)
        {
            this.MakeFolderForWinner = makefolderforwinner;
            this.MakeFolderForLoser = makefolderforloser;
            this.ValidGameTypes = validgametypes;
            this.Durations = durations;
        }

        #endregion

        #region properties

        public bool? MakeFolderForWinner { get; set; }

        public bool? MakeFolderForLoser { get; set; }

        public IDictionary<GameType, bool> ValidGameTypes { get; set; }

        public int[] Durations { get; set; }

        #endregion

        #region methods

        public override string ToString()
        {
            if (string.IsNullOrEmpty(_cachedStringRepresentation))
            {
                StringBuilder validGameTypesAsString = new StringBuilder();
                foreach (var validGameType in ValidGameTypes)
                {
                    validGameTypesAsString.Append($"{validGameType.Key.ToString()} {validGameType.Value.ToString()} ");
                }

                _cachedStringRepresentation = $"MakeFolderForWinner: {MakeFolderForWinner} MakeFolderForLoser: {MakeFolderForLoser} ValidGameTypes: {validGameTypesAsString}Durations: {string.Join(" ", Durations)}";
            }

            return _cachedStringRepresentation;
        }

        #endregion

        #endregion
    }
}

