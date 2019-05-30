using ReplayParser.ReplaySorter.Renaming;
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

        // rename in place:
        // customreplayformat != null 
        // output directory = empty
        // rename in place = true
        // restoreoriginalreplaynames = false

        // rename to output:
        // customreplayformat != null
        // outputdirectory != null/empty
        // renameinplace = false
        // restore = false

        // restore:
        // customreplayformat = null
        // outputdirectory = null
        // renameinplace = false
        // restore = true

        public static RenamingParameters Create(CustomReplayFormat customReplayFormat, string outputDirectory, bool? renameInPlace, bool? restoreOriginalReplayNames)
        {
            var renameInPlaceValue = renameInPlace.HasValue && renameInPlace.Value;
            var restoreOriginalReplayNamesValue = restoreOriginalReplayNames.HasValue && restoreOriginalReplayNames.Value;

            if (customReplayFormat != null)
            {
                if (renameInPlaceValue)
                {
                    if (string.IsNullOrWhiteSpace(outputDirectory))
                        return new RenamingParameters(customReplayFormat, string.Empty, true, false);

                    return null;
                }

                if (string.IsNullOrWhiteSpace(outputDirectory))
                    return null;

                return new RenamingParameters(customReplayFormat, outputDirectory, false, false);
            }
            else
            {
                if (restoreOriginalReplayNamesValue)
                    return new RenamingParameters(null, null, false, true);

                return null;
            }
        }

        public static RenamingParameters Default => new RenamingParameters();

        private RenamingParameters() { }

        private RenamingParameters(CustomReplayFormat customReplayFormat, string outputDirectory, bool renameInPlace, bool restoreOriginalReplayNames)
        {
            _customReplayFormat = customReplayFormat;
            _outputDirectory = outputDirectory;
            _renameInPlace = renameInPlace;
            _restoreOriginalReplayNames = restoreOriginalReplayNames;
        }

        public bool RenameInPlace => _renameInPlace;
        public bool RestoreOriginalReplayNames => _restoreOriginalReplayNames;
        public string OutputDirectory => _outputDirectory;
        public CustomReplayFormat CustomReplayFormat => _customReplayFormat;

        public override string ToString()
        {
            return $"RenameInPlace: {RenameInPlace} RestoreOriginalReplayNames: {RestoreOriginalReplayNames} OutputDirectory: {OutputDirectory} CustomReplayFormat: {CustomReplayFormat}";
        }
    }
}
