using Microsoft.WindowsAPICodePack.Dialogs;
using ReplayParser.ReplaySorter.Diagnostics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ReplayParser.ReplaySorter.UI.Windows
{
    /// <summary>
    /// Interaction logic for Backup.xaml
    /// </summary>
    public partial class BackupWindow : Window
    {
        #region private

        #region fields

        private BackupAction _backupAction;
        private string _backupName;
        private CancellationTokenSource _cancellationTokenSource;

        #endregion

        #region methods

        private void InitializeWindow()
        {
            Title = $"{_backupAction} backup";
            header.Content = GetHeader(_backupAction);
            var actionContent = GetActionContentAsync(_backupAction);
            mainLayoutGrid.Children.Add(actionContent);
        }

        private string GetHeader(BackupAction backupAction)
        {
            switch (backupAction)
            {
                case BackupAction.Create:
                    return "Create new backup";
                case BackupAction.Delete:
                    return $"Delete existing backup: {_backupName}";
                case BackupAction.Inspect:
                    return $"Inspect existing backup: {_backupName}";
                case BackupAction.Restore:
                    return $"Restore from backup: {_backupName}";
                default:
                    throw new ArgumentException(nameof(backupAction));
            }
        }

        private UIElement GetActionContentAsync(BackupAction backupAction)
        {
            Uri actionContentUri = new Uri($"pack://application:,,,/Windows/BackupActions/{backupAction.ToString()}BackupActionContent.xaml", UriKind.Absolute);
            var resourceStreamInfo = Application.GetResourceStream(actionContentUri);
            var xamlReader = new XamlReader();
            //TODO investigate LoadAsync but no awaiter implemented...
            var uiElement = xamlReader.LoadAsync(resourceStreamInfo.Stream) as UIElement;
            AttachEventHandlers(uiElement, backupAction);
            return uiElement;
        }

        private void AttachEventHandlers(UIElement uiElement, BackupAction backupAction)
        {
            switch (backupAction)
            {
                case BackupAction.Create:
                    AttachCreateEventHandlers(uiElement);
                    break;
                case BackupAction.Delete:
                    AttachDeleteEventHandlers(uiElement);
                    break;
                case BackupAction.Inspect:
                    AttachInspectEventHandlers(uiElement);
                    break;
                case BackupAction.Restore:
                    AttachRestoreEventHandlers(uiElement);
                    break;
                default:
                    throw new ArgumentException(nameof(backupAction));
            }
        }

        private void AttachCreateEventHandlers(UIElement uiElement)
        {
            //TODO what would be a good way to make this less error prone, how to associate the names of these elements with the dynamically loaded xaml in a more robust or type safe way?
            var clearReplaysButton = LogicalTreeHelper.FindLogicalNode(uiElement, "clearFoundReplayFilesButton") as Button;
            var importReplaysButton = LogicalTreeHelper.FindLogicalNode(uiElement, "importReplayFilesButton") as Button;

            clearReplaysButton.Click += ClearFoundReplayFilesButton_Click;
            importReplaysButton.Click += ImportReplayFilesButton_Click;
            //LogicalTreeHelper.FindLogicalNode(uiElement, "nameTextBox");
            //LogicalTreeHelper.FindLogicalNode(uiElement, "commentTextBox");
        }

        private void ClearFoundReplayFilesButton_Click(object sender, RoutedEventArgs e)
        {
            var replayFilesFoundListBox = LogicalTreeHelper.FindLogicalNode(this, "replayFilesFoundListBox") as ListBox;
            replayFilesFoundListBox.ItemsSource = null;
        }

        private async void ImportReplayFilesButton_Click(object sender, RoutedEventArgs e)
        {
            var folderDialog = new CommonOpenFileDialog();
            folderDialog.IsFolderPicker = true;
            if (folderDialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                try
                {
                    var folderName = folderDialog.FileName;
                    if (_cancellationTokenSource != null)
                    {
                        MessageBox.Show($"An operation is still in progress, click cancel before startching a new search.", "Invalid operation.", MessageBoxButton.OK, MessageBoxImage.Exclamation, MessageBoxResult.OK);
                        return;
                    }
                    _cancellationTokenSource = new CancellationTokenSource();
                    var token = _cancellationTokenSource.Token;

                    //TODO i guess in theory it would be possible to only retrieve a batch of elements, and if the user scrolls down we search for another batch etc
                    //TODO handle unauthorized exception??
                    var task = Task.Run(() =>
                        {
                            Task.Delay(2000);
                            List<string> files = new List<string>();
                            foreach (var file in Directory.EnumerateFiles(
                                        folderName,
                                        "*",
                                        SearchOption.AllDirectories))
                            {
                                if (token.IsCancellationRequested)
                                    token.ThrowIfCancellationRequested();

                                if (System.IO.Path.GetExtension(file) == ".rep")
                                    files.Add(file);
                            }
                            return files;

                        }, token
                    );

                    Focus();

                    var potentialFiles = await task;

                    if (potentialFiles.Count() == 0)
                    {
                        MessageBox.Show($"No replays found in {folderName}. Please specify an existing directory containing your replays.", "Failed to find replays.", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                        return;
                    }

                    var replayFilesFoundListBox = LogicalTreeHelper.FindLogicalNode(this, "replayFilesFoundListBox") as ListBox;
                    replayFilesFoundListBox.ItemsSource = potentialFiles;
                    replayFilesFoundListBox.Items.Refresh();
                    // statusBarAction.Content = $"Discovered {_files.Count} replays!";


                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Something went wrong while finding replay files: {ex.Message}", "Failed to find replays.", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);
                    ErrorLogger.GetInstance()?.LogError($"{DateTime.Now} - Something went wrong while enumerating files", ex: ex);
                    return;
                }
                finally
                {
                    _cancellationTokenSource.Dispose();
                    _cancellationTokenSource = null;
                }
            }
        }

        private void AttachDeleteEventHandlers(UIElement uiElement)
        {
            throw new NotImplementedException();
        }

        private void AttachInspectEventHandlers(UIElement uiElement)
        {
            throw new NotImplementedException();
        }

        private void AttachRestoreEventHandlers(UIElement uiElement)
        {
            throw new NotImplementedException();
        }

        private void CancelAsyncOperationButton_Click(object sender, RoutedEventArgs e)
        {
            if (_cancellationTokenSource != null)
            {
                _cancellationTokenSource.Cancel();
            }
        }
        #endregion

        #endregion

        #region public

        public ReplayParser.ReplaySorter.Backup.Models.Backup Backup { get; private set; }

        #region constructor

        public BackupWindow(BackupAction backupAction, string backupName)
        {
            InitializeComponent();
            _backupAction = backupAction;
            _backupName = backupName;
            InitializeWindow();
        }

        #endregion

        #region methods

        #endregion

        #endregion

    }
}
