using Microsoft.WindowsAPICodePack.Dialogs;
using ReplayParser.ReplaySorter.Configuration;
using System;
using System.Linq;
using System.Windows;

namespace ReplayParser.ReplaySorter.UI.Windows
{
    /// <summary>
    /// Interaction logic for AdvancedSettings.xaml
    /// </summary>
    public partial class AdvancedSettings : Window
    {
        private IReplaySorterConfiguration _replaySorterConfiguration;

        public AdvancedSettings(IReplaySorterConfiguration replaySorterConfiguration)
        {
            InitializeComponent();
            _replaySorterConfiguration = replaySorterConfiguration;
        }

        private void SaveSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            if (MaxUndoLevelCombobox.SelectedValue != null)
                _replaySorterConfiguration.MaxUndoLevel = (uint)MaxUndoLevelCombobox.SelectedValue;

            _replaySorterConfiguration.CheckForUpdates = AutomaticCheckUpdatesCheckbox.IsChecked.HasValue && AutomaticCheckUpdatesCheckbox.IsChecked.Value;
            _replaySorterConfiguration.RememberParsingDirectory = SaveLastParseDirectoryCheckbox.IsChecked.HasValue && SaveLastParseDirectoryCheckbox.IsChecked.Value;
            _replaySorterConfiguration.IncludeSubDirectoriesByDefault = IncludeSubDirectoriesByDefaultCheckbox.IsChecked.HasValue && IncludeSubDirectoriesByDefaultCheckbox.IsChecked.Value;
            _replaySorterConfiguration.LoadReplaysOnStartup = ParseReplaysOnStartupCheckbox.IsChecked.HasValue && ParseReplaysOnStartupCheckbox.IsChecked.Value;
            _replaySorterConfiguration.CheckForDuplicatesOnCumulativeParsing = CheckForDuplicatesCheckbox.IsChecked.HasValue && CheckForDuplicatesCheckbox.IsChecked.Value;
            _replaySorterConfiguration.IgnoreFilePath = IgnoreFileTextbox.Text;
            _replaySorterConfiguration.LogDirectory = LoggingDirectoryTextbox.Text;
            this.Close();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            InitializeMaximumUndoLevels();
            LoadSettings();
        }

        private void InitializeMaximumUndoLevels()
        {
            MaxUndoLevelCombobox.ItemsSource = Enumerable.Range(1, 50).Select(i => (uint)i);
        }

        private void LoadSettings()
        {
            MaxUndoLevelCombobox.SelectedValue = _replaySorterConfiguration.MaxUndoLevel;
            AutomaticCheckUpdatesCheckbox.IsChecked = _replaySorterConfiguration.CheckForUpdates;
            SaveLastParseDirectoryCheckbox.IsChecked = _replaySorterConfiguration.RememberParsingDirectory;
            IncludeSubDirectoriesByDefaultCheckbox.IsChecked = _replaySorterConfiguration.IncludeSubDirectoriesByDefault;
            ParseReplaysOnStartupCheckbox.IsChecked = _replaySorterConfiguration.LoadReplaysOnStartup;
            CheckForDuplicatesCheckbox.IsChecked = _replaySorterConfiguration.CheckForDuplicatesOnCumulativeParsing;
            IgnoreFileTextbox.Text = _replaySorterConfiguration.IgnoreFilePath;
            LoggingDirectoryTextbox.Text = _replaySorterConfiguration.LogDirectory;
        }

        private void SetIgnoreFileButton_Click(object sender, RoutedEventArgs e)
        {
            var selectIgnoreFile = new CommonOpenFileDialog();
            selectIgnoreFile.EnsureFileExists = true;
            if (selectIgnoreFile.ShowDialog() == CommonFileDialogResult.Ok)
            {
                IgnoreFileTextbox.Text = selectIgnoreFile.FileName;
            }
        }

        private void SetLoggingDirectoryButton_Click(object sender, RoutedEventArgs e)
        {
            var setLoggingDirectory = new CommonOpenFileDialog();
            setLoggingDirectory.IsFolderPicker = true;
            if (setLoggingDirectory.ShowDialog() == CommonFileDialogResult.Ok)
            {
                LoggingDirectoryTextbox.Text = setLoggingDirectory.FileName;
            }
        }
    }
}
