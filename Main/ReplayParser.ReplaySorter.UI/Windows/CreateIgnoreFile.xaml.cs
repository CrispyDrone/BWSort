using Microsoft.WindowsAPICodePack.Dialogs;
using ReplayParser.ReplaySorter.Configuration;
using ReplayParser.ReplaySorter.Diagnostics;
using ReplayParser.ReplaySorter.Ignoring;
using ReplayParser.ReplaySorter.IO;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;

namespace ReplayParser.ReplaySorter.UI.Windows
{
    /// <summary>
    /// Interaction logic for CreateIgnoreFile.xaml
    /// </summary>
    public partial class CreateIgnoreFile : Window
    {
        #region private

        #region fields

        private IReplaySorterConfiguration _replaySorterConfiguration;
        private IgnoreFileManager _ignoreFileManager;
        private IgnoreFile _ignoreFile;

        #endregion

        #region methods

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            editIgnoreFileTextBox.SelectAll();
            editIgnoreFileTextBox.Focus();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(_replaySorterConfiguration.IgnoreFilePath))
            {
                try
                {
                    _ignoreFile = _ignoreFileManager.Load(_replaySorterConfiguration.IgnoreFilePath);
                    var ignoredFiles = new StringBuilder();
                    foreach (var file in _ignoreFile.IgnoredFiles.Select(iFile => iFile.Item1))
                    {
                        ignoredFiles.AppendLine(file);
                    }

                    editIgnoreFileTextBox.Text = ignoredFiles.ToString();
                }
                catch (Exception ex)
                {
                    ErrorLogger.GetInstance()?.LogError($"Failed to load existing ignore file at location {_replaySorterConfiguration.IgnoreFilePath}", ex: ex);
                    MessageBox.Show($"Failed to load existing ignore file at location {_replaySorterConfiguration.IgnoreFilePath}", "Invalid file", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK); ;
                }
            }
        }

        private void SaveEditingIgnoreFileButton_Click(object sender, RoutedEventArgs e)
        {
            var errors = new StringBuilder();
            int counter = 0;
            var safeIgnoreFile = new CommonSaveFileDialog();
            safeIgnoreFile.DefaultExtension = "repignore";
            if (safeIgnoreFile.ShowDialog() == CommonFileDialogResult.Ok)
            {
                var ignoreFile = _ignoreFile ?? new IgnoreFile();
                var toIgnoreFiles = editIgnoreFileTextBox.Text.Split('\n').Select(f => f.Trim(' ', '\t', '\n', '\r'));
                ignoreFile.Clear();
                foreach (var toIgnoreFile in toIgnoreFiles)
                {
                    if (string.IsNullOrWhiteSpace(toIgnoreFile))
                        continue;

                    string hash = null;
                    try
                    {
                        hash = FileHasher.GetMd5Hash(toIgnoreFile);
                        if (hash == null)
                        {
                            counter++;
                            errors.AppendLine($"{counter}. Failed to compute hash for file {toIgnoreFile}");
                            continue;
                        }

                        ignoreFile.Ignore(new Tuple<string, string>(toIgnoreFile, hash));
                    }
                    catch (Exception ex)
                    {
                        ErrorLogger.GetInstance()?.LogError($"An error occurred while trying to compute the hash for file {toIgnoreFile}", ex: ex);
                        counter++;
                        errors.AppendLine($"{counter}. An error occurred while processing the file {toIgnoreFile}");
                    }
                }

                _ignoreFileManager.Save(ignoreFile, safeIgnoreFile.FileName, true);
                _replaySorterConfiguration.IgnoreFilePath = safeIgnoreFile.FileName;
            }
            if (errors.Length > 0)
            {
                MessageBox.Show($"The following replays encountered errors:{Environment.NewLine}{errors.ToString()}", "Invalid files", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);
            }
            this.Close();
        }

        private void ImportFileNamesButton_Click(object sender, RoutedEventArgs e)
        {
            var importFileNamesDialog = new CommonOpenFileDialog();
            importFileNamesDialog.IsFolderPicker = true;
            var toIgnoreFilesBuilder = new StringBuilder();
            if (importFileNamesDialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                var dir = importFileNamesDialog.FileName;
                var toIgnoreFiles = Directory.EnumerateFiles(dir, "*.rep", SearchOption.AllDirectories).Where(f => Path.GetExtension(f) == ".rep");
                foreach (var file in toIgnoreFiles)
                {
                    toIgnoreFilesBuilder.AppendLine(file);
                }
            }
            editIgnoreFileTextBox.Text = editIgnoreFileTextBox.Text + Environment.NewLine + toIgnoreFilesBuilder.ToString();
        }

        private void SelectFileNamesButton_Click(object sender, RoutedEventArgs e)
        {
            var selectFileNamesDialog = new CommonOpenFileDialog();
            selectFileNamesDialog.Multiselect = true;
            var toIgnoreFilesBuilder = new StringBuilder();
            if (selectFileNamesDialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                var toIgnoreFiles = selectFileNamesDialog.FileNames;
                foreach (var file in toIgnoreFiles)
                {
                    toIgnoreFilesBuilder.AppendLine(file);
                }
            }
            editIgnoreFileTextBox.Text = editIgnoreFileTextBox.Text + Environment.NewLine + toIgnoreFilesBuilder.ToString();
        }

        #endregion

        #endregion

        #region public

        #region constructor

        public CreateIgnoreFile(IReplaySorterConfiguration replaySorterConfiguration, IgnoreFileManager ignoreFileManager)
        {
            _replaySorterConfiguration = replaySorterConfiguration;
            _ignoreFileManager = ignoreFileManager;
            InitializeComponent();
        }

        #endregion

        #endregion
    }
}
