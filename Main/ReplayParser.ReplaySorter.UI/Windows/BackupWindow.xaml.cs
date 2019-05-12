using Microsoft.WindowsAPICodePack.Dialogs;
using ReplayParser.ReplaySorter.Backup;
using ReplayParser.ReplaySorter.Diagnostics;
using ReplayParser.ReplaySorter.UI.Models;
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
        private CancellationTokenSource _cancellationTokenSource;
        private BWContext _activeUow;
        private BackupWithCount _backupWithCount;

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
                    return $"Delete existing backup: {_backupWithCount?.Name ?? string.Empty}";
                case BackupAction.Inspect:
                    return $"Inspect existing backup: {_backupWithCount?.Name ?? string.Empty}";
                case BackupAction.Restore:
                    return $"Restore from backup: {_backupWithCount?.Name ?? string.Empty}";
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
            AttachEventHandlersAndDataBinding(uiElement, backupAction);
            return uiElement;
        }

        private void AttachEventHandlersAndDataBinding(UIElement uiElement, BackupAction backupAction)
        {
            switch (backupAction)
            {
                case BackupAction.Create:
                    AttachCreateEventHandlersAndDataBinding(uiElement);
                    break;
                case BackupAction.Delete:
                    AttachDeleteEventHandlersAndDataBinding(uiElement);
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

        private void AttachCreateEventHandlersAndDataBinding(UIElement uiElement)
        {
            //TODO what would be a good way to make this less error prone, how to associate the names of these elements with the dynamically loaded xaml in a more robust or type safe way?
            var clearReplaysButton = LogicalTreeHelper.FindLogicalNode(uiElement, "clearFoundReplayFilesButton") as Button;
            var importReplaysButton = LogicalTreeHelper.FindLogicalNode(uiElement, "importReplayFilesButton") as Button;
            var createBackupButton = LogicalTreeHelper.FindLogicalNode(uiElement, "createBackupButton") as Button;

            clearReplaysButton.Click += ClearFoundReplayFilesButton_Click;
            importReplaysButton.Click += ImportReplayFilesButton_Click;
            createBackupButton.Click += CreateBackupButton_Click;
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
                    backupProgressBarLabel.Content = "Searching for replays...";
                    backupProgressBar.IsIndeterminate = true;
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
                    backupProgressBarLabel.Content = $"Finished searching for replays!";
                    backupProgressBar.IsIndeterminate = false;

                    if (potentialFiles.Count() == 0)
                    {
                        MessageBox.Show($"No replays found in {folderName}. Please specify an existing directory containing your replays.", "Failed to find replays.", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                        return;
                    }

                    var replayFilesFoundListBox = LogicalTreeHelper.FindLogicalNode(this, "replayFilesFoundListBox") as ListBox;
                    replayFilesFoundListBox.ItemsSource = potentialFiles;
                    replayFilesFoundListBox.Items.Refresh();
                    var rootDirectoryLabel = LogicalTreeHelper.FindLogicalNode(this, "rootDirectoryLabel") as Label;
                    rootDirectoryLabel.Content = folderName;
                    var replaysFoundMessageLabel = LogicalTreeHelper.FindLogicalNode(this, "replaysFoundMessageLabel") as Label;
                    replaysFoundMessageLabel.Content = potentialFiles.Count; 
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

        private async void CreateBackupButton_Click(object sender, RoutedEventArgs e)
        {
            var replayFilesFoundListBox = LogicalTreeHelper.FindLogicalNode(this, "replayFilesFoundListBox") as ListBox;
            if (replayFilesFoundListBox.Items.Count == 0)
            {
                MessageBox.Show("No replays have been found. Import a directory with replays first before attempting to create a backup.", "Invalid operation", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);
                return;
            }

            var backupName = (LogicalTreeHelper.FindLogicalNode(this, "nameTextBox") as TextBox).Text;
            var backupComment = (LogicalTreeHelper.FindLogicalNode(this, "commentTextBox") as TextBox).Text;
            var rootDirectory = (LogicalTreeHelper.FindLogicalNode(this, "rootDirectoryLabel") as Label).Content as string;

            var replayFiles = replayFilesFoundListBox.ItemsSource.Cast<string>();

            //NOTE: I don't think i will be able to report progress for these things... so use a "busy indicator" instead of a progress bar
            backupProgressBar.IsIndeterminate = true;
            backupProgressBarLabel.Content = "Creating backup...";
            await Task.Run(() => CreateBackup(backupName, backupComment, rootDirectory, replayFiles));
            backupProgressBarLabel.Content = "Finished creating backup!";
            backupProgressBar.IsIndeterminate = false;
            DialogResult = true;
        }

        private void CreateBackup(string name, string comment, string rootDirectory, IEnumerable<string> replayFiles)
        {
            // Dispatcher.Invoke(() => backupProgressBarLabel.Content = "Creating backup...");
            var dbName = _activeUow.DatabaseName;
            var createBackup = Models.CreateBackup.Create(name, comment, rootDirectory, replayFiles);
            var backupId = _activeUow.BackupRepository.Create(createBackup.ToBackup());
            _activeUow.Commit();
            using (var uow = BWContext.Create(dbName, false))
            {
                var backup = uow.BackupRepository.Get(backupId);
                var count = uow.BackupRepository.GetNumberOfBackedUpReplays(backupId).Value;
                _backupWithCount = new BackupWithCount
                {
                    Id = backup.Id,
                    Name = backup.Name,
                    Comment = backup.Comment,
                    RootDirectory = backup.RootDirectory,
                    Date = backup.Date,
                    Count = count
                };
            }
            // Dispatcher.Invoke(() => backupProgressBarLabel.Content = "Finished creating backup!");
        }

        private void AttachDeleteEventHandlersAndDataBinding(UIElement uiElement)
        {
            if (_backupWithCount == null)
                throw new InvalidOperationException("Failed to load backup");

            var deleteButton = LogicalTreeHelper.FindLogicalNode(uiElement, "deleteBackupButton") as Button;
            deleteButton.Click += DeleteBackup_Click;

            var backupIdLabel = LogicalTreeHelper.FindLogicalNode(uiElement, "backupIdLabel") as Label;
            var nameLabel = LogicalTreeHelper.FindLogicalNode(uiElement, "backupNameLabel") as Label;
            var commentLabel = LogicalTreeHelper.FindLogicalNode(uiElement, "backupCommentLabel") as Label;
            var rootDirectoryLabel = LogicalTreeHelper.FindLogicalNode(uiElement, "backupRootDirectoryLabel") as Label;
            var dateLabel = LogicalTreeHelper.FindLogicalNode(uiElement, "backupDateLabel") as Label;
            var countLabel = LogicalTreeHelper.FindLogicalNode(uiElement, "backupCountLabel") as Label;

            backupIdLabel.Content = _backupWithCount.Id;
            nameLabel.Content = _backupWithCount.Name;
            commentLabel.Content = _backupWithCount.Comment;
            rootDirectoryLabel.Content = _backupWithCount.RootDirectory;
            dateLabel.Content = _backupWithCount.Date;
            countLabel.Content = _backupWithCount.Count;
        }

        private void DeleteBackup_Click(object source, RoutedEventArgs e)
        {
            if (_backupWithCount == null)
                throw new NullReferenceException("Failed to load backup. Unable to delete.");

            //TODO
            var backupId = _backupWithCount.Id;
            var deleteOrphanReplaysCheckbox = LogicalTreeHelper.FindLogicalNode(this, "deleteOrphanReplays") as CheckBox;
            var deleteOrphanReplays = deleteOrphanReplaysCheckbox.IsChecked.HasValue && deleteOrphanReplaysCheckbox.IsChecked.Value;

            backupProgressBar.IsIndeterminate = true;
            backupProgressBarLabel.Content = "Deleting backup...";
            if (deleteOrphanReplays)
            {
                _activeUow.BackupRepository.RemoveWithOrphanReplays(backupId);
            }
            else
            {
                _activeUow.BackupRepository.Remove(backupId);
            }
            //TODO once you commit, _activeUow becomes useless so maybe you should automatically close the window or prevent them from pressing delete again etc...
            _activeUow.Commit();
            backupProgressBar.IsIndeterminate = false;
            backupProgressBarLabel.Content = "Successfully deleted backup...";
            DialogResult = true;
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

        public BackupWithCount Backup
        {
            get => _backupWithCount;
            private set
            {
                _backupWithCount = value;
            }
        }

        #region constructor

        public BackupWindow(BackupAction backupAction, BackupWithCount backupWithCount, BWContext bwContext)
        {
            InitializeComponent();
            _backupWithCount = backupWithCount;
            _backupAction = backupAction;
            _activeUow = bwContext;
            InitializeWindow();
        }

        #endregion

        #region methods

        #endregion

        #endregion

    }
}
