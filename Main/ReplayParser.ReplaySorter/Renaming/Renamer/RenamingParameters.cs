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
        private bool _isPreview;
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

        public static RenamingParameters Create(CustomReplayFormat customReplayFormat, string outputDirectory, bool? renameInPlace, bool? restoreOriginalReplayNames, bool? isPreview)
        {
            var renameInPlaceValue = renameInPlace.HasValue && renameInPlace.Value;
            var restoreOriginalReplayNamesValue = restoreOriginalReplayNames.HasValue && restoreOriginalReplayNames.Value;
            var isPreviewValue = isPreview.HasValue && isPreview.Value;

            if (customReplayFormat != null)
            {
                if (renameInPlaceValue)
                {
                    if (string.IsNullOrWhiteSpace(outputDirectory))
                        return new RenamingParameters(customReplayFormat, string.Empty, true, false, isPreviewValue);

                    return null;
                }

                if (string.IsNullOrWhiteSpace(outputDirectory))
                    return null;

                return new RenamingParameters(customReplayFormat, outputDirectory, false, false, isPreviewValue);
            }
            else
            {
                if (restoreOriginalReplayNamesValue)
                    return new RenamingParameters(null, null, false, true, isPreviewValue);

                return null;
            }
        }

        public static RenamingParameters Default => new RenamingParameters();

        private RenamingParameters() { }

        private RenamingParameters(CustomReplayFormat customReplayFormat, string outputDirectory, bool renameInPlace, bool restoreOriginalReplayNames, bool isPreview)
        {
            _customReplayFormat = customReplayFormat;
            _outputDirectory = outputDirectory;
            _renameInPlace = renameInPlace;
            _restoreOriginalReplayNames = restoreOriginalReplayNames;
            _isPreview = isPreview;
        }

        public bool RenameInPlace => _renameInPlace;
        public bool RestoreOriginalReplayNames => _restoreOriginalReplayNames;
        public bool IsPreview => _isPreview;
        public string OutputDirectory => _outputDirectory;
        public CustomReplayFormat CustomReplayFormat => _customReplayFormat;

        public override string ToString()
        {
            return $"RenameInPlace: {RenameInPlace} RestoreOriginalReplayNames: {RestoreOriginalReplayNames} IsPreview: {IsPreview} OutputDirectory: {OutputDirectory} CustomReplayFormat: {CustomReplayFormat}";
        }
    }
}
