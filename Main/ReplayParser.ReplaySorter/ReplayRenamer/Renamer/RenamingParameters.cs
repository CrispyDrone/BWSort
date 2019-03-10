using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReplayParser.ReplaySorter.ReplayRenamer
{
    public class RenamingParameters
    {
        private bool _renameInPlace;
        private bool _renameLastSort;
        private string _outputDirectory;
        private CustomReplayFormat _customReplayFormat;
        private string _originalDirectory;

        public static RenamingParameters Create(CustomReplayFormat customReplayFormat, string originalDirectory, string outputDirectory, bool? renameInPlace, bool? renameLastSort)
        {
            if (customReplayFormat == null) return null;

            var renameInPlaceValue = renameInPlace.HasValue && renameInPlace.Value;
            var renameLastSortValue = renameLastSort.HasValue && renameLastSort.Value;

            if (!(renameInPlaceValue || renameLastSortValue))
            {
                if (string.IsNullOrWhiteSpace(outputDirectory))
                {
                    return null;
                }
                return new RenamingParameters(customReplayFormat, originalDirectory, outputDirectory, false, false);
            }

            // null + null => bad
            // false + null => bad
            // null + false => bad
            // false + false => bad
            // true + true => bad
            // null + true => good
            // true + null => good
            // true + false => good
            // false + true => good

            if (!(renameInPlaceValue ^ renameLastSortValue)) return null;

            return new RenamingParameters(customReplayFormat, string.Empty, outputDirectory, renameInPlaceValue, renameLastSortValue);
        }

        public static RenamingParameters Default => new RenamingParameters();

        private RenamingParameters() { }

        private RenamingParameters(CustomReplayFormat customReplayFormat, string originalDirectory, string outputDirectory, bool renameInPlace, bool renameLastSort)
        {
            _customReplayFormat = customReplayFormat;
            _originalDirectory = originalDirectory;
            _outputDirectory = outputDirectory;
            _renameInPlace = renameInPlace;
            _renameLastSort = renameLastSort;
        }

        public bool RenameInPlace => _renameInPlace;
        public bool RenameLastSort => _renameLastSort;
        public string OriginalDirectory => _originalDirectory;
        public string OutputDirectory => _outputDirectory;
        public CustomReplayFormat CustomReplayFormat => _customReplayFormat;

        public override string ToString()
        {
            return $"RenameInPlace: {RenameInPlace} RenameLastSort: {RenameLastSort} OutputDirectory: {OutputDirectory} CustomReplayFormat: {CustomReplayFormat} OriginalDirectory: {OriginalDirectory}";
        }
    }
}
