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

        #region methods

        private static bool GameTypesEqual(IDictionary<GameType, bool> validGameTypes1, IDictionary<GameType, bool> validGameTypes2)
        {
            if (validGameTypes1 == null ^ validGameTypes2 == null)
                return false;

            if (validGameTypes1 == null && validGameTypes2 == null)
                return true;

            if (validGameTypes1.Count != validGameTypes2.Count)
                return false;

            foreach (var kvp in validGameTypes1)
            {
                if (validGameTypes2[kvp.Key] != kvp.Value)
                    return false;
            }

            return true;
        }

        private static bool DurationsEqual(int[] durations1, int[] durations2)
        {
            if (durations1 == null ^ durations2 == null)
                return false;

            if (durations1 == null && durations2 == null)
                return true;

            if (durations1.Count() != durations2.Count())
                return false;

            return durations1.SequenceEqual(durations2);
        }

        private int GetHashCode(IDictionary<GameType, bool> validGameTypes)
        {
            if (validGameTypes == null)
                return 0;

            return validGameTypes.Count.GetHashCode() * 17 + validGameTypes.Where(kvp => kvp.Value == true).Count().GetHashCode();
        }

        private int GetHashCode(int[] durations)
        {
            if (durations == null)
                return 0;

            return durations.Count().GetHashCode() + durations.Select((v, i) => i * v * 17).Aggregate((x, y) => x + y).GetHashCode();
        }

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
                if (ValidGameTypes != null)
                {
                    foreach (var validGameType in ValidGameTypes)
                    {
                        validGameTypesAsString.Append($"{validGameType.Key.ToString()} {validGameType.Value.ToString()} ");
                    }
                }

                _cachedStringRepresentation = $"MakeFolderForWinner: {MakeFolderForWinner?.ToString() ?? string.Empty} MakeFolderForLoser: {MakeFolderForLoser?.ToString() ?? string.Empty} ValidGameTypes: {validGameTypesAsString.ToString()}Durations: {(Durations == null ? string.Empty : string.Join(" ", Durations))}";
            }

            return _cachedStringRepresentation;
        }

        #region overriding

        public override bool Equals(object obj)
        {
            var parameters = obj as SortCriteriaParameters;
            return parameters != null &&
                this == parameters;
        }

        //TODO change
        public override int GetHashCode()
        {
            var hashCode = -303538154;
            hashCode = hashCode * -1521134295 + EqualityComparer<bool?>.Default.GetHashCode(MakeFolderForWinner);
            hashCode = hashCode * -1521134295 + EqualityComparer<bool?>.Default.GetHashCode(MakeFolderForLoser);
            hashCode = hashCode * -1521134295 + GetHashCode(ValidGameTypes);
            hashCode = hashCode * -1521134295 + GetHashCode(Durations);
            return hashCode;
        }

        #endregion

        #endregion

        #region overloading

        public static bool operator == (SortCriteriaParameters x, SortCriteriaParameters y)
        {
            return
                DurationsEqual(x.Durations, y.Durations) &&
                x.MakeFolderForLoser == y.MakeFolderForLoser &&
                x.MakeFolderForWinner == y.MakeFolderForWinner &&
                GameTypesEqual(x.ValidGameTypes, y.ValidGameTypes);
        }
        public static bool operator != (SortCriteriaParameters x, SortCriteriaParameters y)
        {
            return !(x == y);

        }

        #endregion

        #endregion
    }
}

