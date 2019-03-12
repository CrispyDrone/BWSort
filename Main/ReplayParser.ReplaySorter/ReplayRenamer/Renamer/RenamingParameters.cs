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
        private bool _restoreOriginalReplayNames;
        private string _outputDirectory;
        private CustomReplayFormat _customReplayFormat;
        private string _originalDirectory;

        public static RenamingParameters Create(CustomReplayFormat customReplayFormat, string originalDirectory, string outputDirectory, bool? renameInPlace, bool? restoreOriginalReplayNames)
        {
            if (customReplayFormat == null) return null;

            var renameInPlaceValue = renameInPlace.HasValue && renameInPlace.Value;
            var restoreOriginalReplayNamesValue = restoreOriginalReplayNames.HasValue && restoreOriginalReplayNames.Value;

            if (!(renameInPlaceValue || restoreOriginalReplayNamesValue))
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

            if (!(renameInPlaceValue ^ restoreOriginalReplayNamesValue)) return null;

            return new RenamingParameters(customReplayFormat, string.Empty, outputDirectory, renameInPlaceValue, restoreOriginalReplayNamesValue);
        }

        public static RenamingParameters Default => new RenamingParameters();

        private RenamingParameters() { }

        private RenamingParameters(CustomReplayFormat customReplayFormat, string originalDirectory, string outputDirectory, bool renameInPlace, bool restoreOriginalReplayNames)
        {
            _customReplayFormat = customReplayFormat;
            _originalDirectory = originalDirectory;
            _outputDirectory = outputDirectory;
            _renameInPlace = renameInPlace;
            _restoreOriginalReplayNames = restoreOriginalReplayNames;
        }

        public bool RenameInPlace => _renameInPlace;
        public bool RestoreOriginalReplayNames => _restoreOriginalReplayNames;
        public string OriginalDirectory => _originalDirectory;
        public string OutputDirectory => _outputDirectory;
        public CustomReplayFormat CustomReplayFormat => _customReplayFormat;

        public override string ToString()
        {
            return $"RenameInPlace: {RenameInPlace} RestoreOriginalReplayNames: {RestoreOriginalReplayNames} OutputDirectory: {OutputDirectory} CustomReplayFormat: {CustomReplayFormat} OriginalDirectory: {OriginalDirectory}";
        }
    }
}
