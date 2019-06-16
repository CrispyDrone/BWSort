using Microsoft.WindowsAPICodePack.Dialogs;
using ReplayParser.Interfaces;
using ReplayParser.Loader;
using ReplayParser.ReplaySorter.Backup;
using ReplayParser.ReplaySorter.Configuration;
using ReplayParser.ReplaySorter.Diagnostics;
using ReplayParser.ReplaySorter.Extensions;
using ReplayParser.ReplaySorter.Filtering;
using ReplayParser.ReplaySorter.IO;
using ReplayParser.ReplaySorter.Ignoring;
using ReplayParser.ReplaySorter.ReplayRenamer;
using ReplayParser.ReplaySorter.Sorting.SortResult;
using ReplayParser.ReplaySorter.Sorting;
using ReplayParser.ReplaySorter.UI.Models;
using ReplayParser.ReplaySorter.UI.Sorting;
using ReplayParser.ReplaySorter.UI.Windows;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows;
using System;
using ReplayParser.ReplaySorter.Renaming;

namespace ReplayParser.ReplaySorter.UI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region fields

        // configuration
        private IReplaySorterConfiguration _replaySorterConfiguration;

        // parsing
        private string _replayDirectory;
        private List<File<IReplay>> _listReplays;
        private List<ParseFile> _files = new List<ParseFile>();
        private HashSet<string> _replayHashes = new HashSet<string>();
        private List<string> _replaysThrowingExceptions = new List<string>();
        private BackgroundWorker _worker_ReplayParser = null;
        private bool _moveBadReplays = false;
        // this feels silly, you can only use the state object passed to RunWorkAsync in ReportProgress
        private string _errorMessage = string.Empty;
        private string _badReplayDirectory = string.Empty;
        private IgnoreFileManager _ignoreFileManager = new IgnoreFileManager();

        // sorting
        private Sorter _sorter;
        private BackgroundWorker _worker_ReplaySorter = null;
        private bool _sortingReplays = false;
        private bool _keepOriginalReplayNames = true;
        private BoolAnswer _boolAnswer = null;
        private Stopwatch _swSort = new Stopwatch();
        private bool _isDragging = false;
        private Tuple<string[], SortCriteriaParameters, CustomReplayFormat, List<File<IReplay>>> _previewSortArguments = null;
        private DirectoryFileTree _previewTree = null;

        // renaming
        private BackgroundWorker _worker_ReplayRenamer = null;
        private bool _renamingReplays = false;
        private bool _renamingToOutputDirectory = false;
        private bool _renamingIsPreview;

        // undoing
        private BackgroundWorker _worker_Undoer = null;
        private bool _undoingRename = false;

        // renaming + undoing
        private BackgroundWorker _activeWorker = null;

        // renaming and undoing
        private LinkedList<IEnumerable<File<IReplay>>> _renamedReplaysList = new LinkedList<IEnumerable<File<IReplay>>>();
        private LinkedListNode<IEnumerable<File<IReplay>>> _renamedReplayListHead;

        // filtering
        private List<File<IReplay>> _filteredListReplays;

        #endregion

        #region setup

        public MainWindow()
        {
            InitializeComponent();
            EnableSortingAndRenamingButtons(ReplayAction.Parse, false);
            _replaySorterConfiguration = new ReplaySorterAppConfiguration();
            IntializeErrorLogger(_replaySorterConfiguration);
            ReloadDatabaseComboBox();
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (_replaySorterConfiguration.CheckForUpdates)
            {
                using (HttpClient client = new HttpClient())
                {
                    try
                    {
                        client.DefaultRequestHeaders.UserAgent.Add(new System.Net.Http.Headers.ProductInfoHeaderValue("BWSort", _replaySorterConfiguration.Version));
                        string responseBody = await client.GetStringAsync(_replaySorterConfiguration.GithubAPIRepoUrl + @"/releases/latest");
                        var versionTag = _replaySorterConfiguration.VersionRegex.Match(responseBody).Groups[1].Value;
                        var remoteVersion = double.Parse(versionTag);
                        var localVersion = double.Parse(_replaySorterConfiguration.Version);
                        if (localVersion < remoteVersion)
                            MessageBox.Show($"A new version is available at {_replaySorterConfiguration.RepositoryUrl}");
                    }
                    catch (Exception ex)
                    {
                        statusBarErrors.Content = "Failed to check for updates.";
                        ErrorLogger.GetInstance()?.LogError($"{DateTime.Now} - Failed to check for updates.", ex: ex);
                    }
                }
            }
            if (_replaySorterConfiguration.RememberParsingDirectory)
            {
                replayDirectoryTextBox.Text = _replaySorterConfiguration.LastParsingDirectory;
            }
            if (_replaySorterConfiguration.IncludeSubDirectoriesByDefault)
            {
                includeSubdirectoriesCheckbox.IsChecked = true;
            }
            if (_replaySorterConfiguration.LoadReplaysOnStartup)
            {
                await DiscoverReplayFiles();
                parseReplays();
            }
        }

        private void IntializeErrorLogger(IReplaySorterConfiguration replaySorterConfiguration)
        {
            if (ErrorLogger.GetInstance(replaySorterConfiguration) == null)
            {
                MessageBox.Show("Issue intializing logger. Logging will be disabled.", "Logger failure", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);
            }
        }

        #endregion

        #region window closing

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to close BWSort?", "Close BWSort", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) == MessageBoxResult.No)
            {
                e.Cancel = true;
            }
            //TODO save after completing parsing instead??
            if (_replaySorterConfiguration.RememberParsingDirectory)
            {
                _replaySorterConfiguration.LastParsingDirectory = replayDirectoryTextBox.Text;
            }
        }

        private void MenuItemClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        #endregion

        #region parsing

        private class ParseFile
        {
            public string Path { get; set; }
            public FeedBack Feedback { get; set; }
        }

        private async Task DiscoverReplayFiles()
        {
            if (!Directory.Exists(replayDirectoryTextBox.Text))
            {
                MessageBox.Show("Please select an existing directory first.", "Invalid directory", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);
                return;
            }

            SearchOption searchOption = SearchOption.TopDirectoryOnly;
            if (includeSubdirectoriesCheckbox.IsChecked.HasValue && includeSubdirectoriesCheckbox.IsChecked.Value)
                searchOption = SearchOption.AllDirectories;

            _replayDirectory = replayDirectoryTextBox.Text;

            var task = Task.Run(() => Directory.GetFiles(_replayDirectory, "*.rep", searchOption).Where(file => Path.GetExtension(file) == ".rep"));
            var potentialfiles = await task;
            if (applyIgnoreFilesCheckbox.IsChecked.HasValue && applyIgnoreFilesCheckbox.IsChecked.Value)
            {
                var ignoredFiles = _ignoreFileManager.Load(_replaySorterConfiguration.IgnoreFilePath)?.IgnoredFiles.Select(f => f.Item2);
                if (!(ignoredFiles == null || ignoredFiles.Count() == 0))
                {
                    var ignoredFilesSetHashes = new HashSet<string>(ignoredFiles);
                    potentialfiles = potentialfiles.Where(f => !ignoredFilesSetHashes.Contains(HashReplay(f)));
                }
            }

            if (potentialfiles.Count() == 0)
            {
                MessageBox.Show($"No replays found in {_replayDirectory}. Please specify an existing directory containing your replays.", "Failed to find replays.", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }

            _files.AddRange(potentialfiles.Select(f => new ParseFile { Path = f, Feedback = FeedBack.NONE}));

            replayFilesFoundListBox.ItemsSource = _files;
            replayFilesFoundListBox.Items.Refresh();
            statusBarAction.Content = $"Discovered {_files.Count} replays!";
        }

        private void ClearFoundReplayFilesButton_Click(object sender, RoutedEventArgs e)
        {
            ResetReplayParsingVariables(false, true);
            ResetFiltering();
            replayFilesFoundListBox.ItemsSource = null;
            _files.Clear();
        }

        private void parseReplaysButton_Click(object sender, RoutedEventArgs e)
        {
            parseReplays();
        }

        private void parseReplays()
        {
            if (_worker_ReplayParser != null && _worker_ReplayParser.IsBusy)
                return;

            if ((_files?.Count ?? 0) == 0)
            {
                MessageBox.Show("Please discover replay files by setting a directory and clicking \"Add\" before attempting to parse replays.", "Invalid start conditions.", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);
                return;
            }

            statusBarAction.Content = "Parsing replays...";
            if (moveBadReplaysCheckBox.IsChecked == true)
            {
                _moveBadReplays = true;
                _badReplayDirectory = moveBadReplaysDirectory.Text;
                if (!Directory.Exists(_badReplayDirectory))
                {
                    MessageBox.Show("The specified bad replay directory does not exist.", "Invalid directory", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);
                    return;
                }
            }

            if (_worker_ReplayParser == null)
            {
                _worker_ReplayParser = new BackgroundWorker();
                _worker_ReplayParser.WorkerReportsProgress = true;
                _worker_ReplayParser.WorkerSupportsCancellation = true;
                _worker_ReplayParser.DoWork += worker_ParseReplays;
                _worker_ReplayParser.ProgressChanged += worker_ProgressChangedParsingReplays;
                _worker_ReplayParser.RunWorkerCompleted += worker_ParsingReplaysCompleted;
            }
            // sigh... should I make some sort of new class that contains all the properties I want to access during the DoWork ??
            _worker_ReplayParser.RunWorkerAsync(_files);
        }

        private void cancelParsingButton_Click(object sender, RoutedEventArgs e)
        {
            if (_worker_ReplayParser != null && _worker_ReplayParser.IsBusy)
            {
                _worker_ReplayParser.CancelAsync();
            }
        }

        private void worker_ParseReplays(object sender, DoWorkEventArgs e)
        {
            var Potentialfiles = e.Argument as List<string>;

            ResetReplayParsingVariables(false, false);
            Stopwatch sw = new Stopwatch();
            sw.Start();

            int currentPosition = 0;
            int progressPercentage = 0;
            var parsedReplays = new List<File<IReplay>>();
            var hashedReplays = new HashSet<string>();
            var checkForDuplicates = _replaySorterConfiguration.CheckForDuplicatesOnCumulativeParsing;
            uint numberOfDuplicates = 0;

            if (checkForDuplicates)
            {
                (sender as BackgroundWorker).ReportProgress(0, "Verifying parsed replays for hashes...");
                HashCurrentlyParsedReplays(_listReplays, _worker_ReplayParser);
            }

            foreach (var replay in _files)
            {
                if (_worker_ReplayParser.CancellationPending == true)
                {
                    e.Cancel = true;
                    return;
                }
                currentPosition++;
                progressPercentage = Convert.ToInt32(((double)currentPosition / _files.Count()) * 100);
                (sender as BackgroundWorker).ReportProgress(progressPercentage, "Parsing replays...");
                ParseReplay(parsedReplays, hashedReplays, replay, checkForDuplicates, ref numberOfDuplicates);
            }

            _replayHashes?.UnionWith(hashedReplays);

            if (_listReplays == null)
            {
                _listReplays = new List<File<IReplay>>(parsedReplays);
            }
            else
            {
                _listReplays?.AddRange(parsedReplays);
            }

            sw.Stop();
            _errorMessage = string.Empty;
            ReplayHandler.LogBadReplays(_replaysThrowingExceptions, _replaySorterConfiguration.LogDirectory, $"{DateTime.Now} - Error while parsing replay: {{0}}");
            if (_moveBadReplays == true)
            {
                currentPosition = 0;
                progressPercentage = 0;

                foreach (var replay in _replaysThrowingExceptions)
                {
                    currentPosition++;
                    progressPercentage = Convert.ToInt32(((double)currentPosition / _replaysThrowingExceptions.Count()) * 100);
                    (sender as BackgroundWorker).ReportProgress(progressPercentage, "Moving bad replays...");
                    ReplayHandler.RemoveBadReplay(_badReplayDirectory + @"\BadReplays", replay);
                }
            }
            //TODO is this still necessary? I don't think so...
            // _files = _files.Where(x => !_replaysThrowingExceptions.Contains(x)).ToList();
            e.Result = new Tuple<List<File<IReplay>>, TimeSpan, uint>(parsedReplays, sw.Elapsed, numberOfDuplicates);
        }

        private void HashCurrentlyParsedReplays(List<File<IReplay>> listReplays, BackgroundWorker worker_ReplayParser)
        {
            if (listReplays == null || listReplays.Count() == 0)
                return;

            foreach (var replay in listReplays)
            {
                if (worker_ReplayParser.CancellationPending == true)
                    return;

                if (string.IsNullOrEmpty(replay.Hash))
                {
                    replay.Hash = HashReplay(replay.OriginalFilePath);
                }
            }
        }

        //TODO failed replays will still be added to hashed set... it this problematic? I don't think so.
        private void ParseReplay(List<File<IReplay>> parsedReplays, HashSet<string> hashedReplays, ParseFile replay, bool checkForDuplicatesOnCumulativeParsing, ref uint numberOfDuplicates)
        {
            try
            {
                string hashedReplay = null;

                if (checkForDuplicatesOnCumulativeParsing)
                {
                    hashedReplay = HashReplay(replay.Path);
                    if (_replayHashes.Contains(hashedReplay))
                    {
                        numberOfDuplicates++;
                        return;
                    }

                    hashedReplays.Add(hashedReplay);
                }

                ParseReplay(parsedReplays, replay.Path, hashedReplay);
                replay.Feedback = FeedBack.OK;
            }
            catch (Exception)
            {
                replay.Feedback = FeedBack.FAILED;
                _replaysThrowingExceptions.Add(replay.Path);
                _errorMessage = string.Format("Error with replay {0}", replay.Path);
            }
        }

        private string HashReplay(string replay)
        {
            if (!File.Exists(replay))
                throw new FileNotFoundException($"Could not find replay {replay}!");

            return FileHasher.GetMd5Hash(File.ReadAllBytes(replay));
        }

        private void ParseReplay(List<File<IReplay>> parsedReplays, string replay, string hash = null)
        {
            var ParsedReplay = ReplayLoader.LoadReplay(replay);
            parsedReplays.Add(File<IReplay>.Create(ParsedReplay, replay, hash));
        }

        private void worker_ProgressChangedParsingReplays(object sender, ProgressChangedEventArgs e)
        {
            progressBarParsingReplays.Value = e.ProgressPercentage;

            if (_errorMessage != string.Empty)
            {
                statusBarErrors.Content = _errorMessage;
            }
            if (e.UserState != null)
            {
                statusBarAction.Content = e.UserState as string;
            }
        }

        private void worker_ParsingReplaysCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Cancelled == true)
            {
                statusBarAction.Content = "Parsing cancelled...";
                ResetReplayParsingVariables(false, true);
                progressBarParsingReplays.Value = 0;
            }
            else
            {
                var result = e.Result as Tuple<List<File<IReplay>>, TimeSpan, uint>;

                statusBarAction.Content = string.Format("Finished parsing!");
                MessageBox.Show(
                    string.Format("Parsing replays finished! It took {0} to parse {1} replays. {2} replays encountered exceptions. {3} duplicates were found. {4}",
                        result.Item2,
                        result.Item1.Count,
                        _replaysThrowingExceptions.Count(),
                        result.Item3,
                        _moveBadReplays ? "Bad replays have been moved to the specified directory." : ""),
                    "Parsing summary",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information,
                    MessageBoxResult.OK);
                ResetReplayParsingVariables(false, true);
                EnableSortingAndRenamingButtons(ReplayAction.Parse, true);
                listViewReplays.ItemsSource = _listReplays;
                listViewReplays.Items.Refresh();
                replayFilesFoundListBox.Items.Refresh();
            }
        }

        private void ResetReplayParsingVariables(bool clearListReplays, bool resetMoveBadReplays)
        {
            if (clearListReplays)
            {
                _listReplays?.Clear();
            }
            if (resetMoveBadReplays)
            {
                _moveBadReplays = false;
            }
            _replaysThrowingExceptions?.Clear();
        }
        
        private void replayDirectoryButton_Click(object sender, RoutedEventArgs e)
        {
            SelectMapFolder(replayDirectoryTextBox);
        }

        private void badReplayDirectory_Click(object sender, RoutedEventArgs e)
        {
            SelectMapFolder(moveBadReplaysDirectory);
        }

        private void setReplayDirectoryMenuItem_Click(object sender, RoutedEventArgs e)
        {
            SelectMapFolder(replayDirectoryTextBox);
        }

        private void moveBadReplaysDirectoryMenuItem_Click(object sender, RoutedEventArgs e)
        {
            SelectMapFolder(moveBadReplaysDirectory);
        }

        private async void AddNewReplayFilesButton_Click(object sender, RoutedEventArgs e)
        {
            await DiscoverReplayFiles();
        }

        private void CreateNewIgnoreFile_Click(object sender, RoutedEventArgs e)
        {
            var createNewIgnoreFileDialog = new CreateIgnoreFile(_replaySorterConfiguration, _ignoreFileManager);
            createNewIgnoreFileDialog.Show();
        }

        #endregion

        #region util

        private void EnableSortingAndRenamingButtons(ReplayAction action, bool enable)
        {
            switch (action)
            {
                case ReplayAction.Parse:
                    executeSortButton.IsEnabled = enable;
                    cancelSortButton.IsEnabled = enable;
                    executeRenamingButton.IsEnabled = enable;
                    cancelRenamingButton.IsEnabled = enable;
                    undoRenamingButton.IsEnabled = enable;
                    redoRenamingButton.IsEnabled = enable;
                    // renameInPlaceCheckBox.IsEnabled = enable;
                    // restoreOriginalReplayNamesCheckBox.IsEnabled = enable;
                    return;
                case ReplayAction.Sort:
                    parseReplaysButton.IsEnabled = enable;
                    cancelParsingButton.IsEnabled = enable;
                    executeRenamingButton.IsEnabled = enable;
                    cancelRenamingButton.IsEnabled = enable;
                    undoRenamingButton.IsEnabled = enable;
                    redoRenamingButton.IsEnabled = enable;
                    // renameInPlaceCheckBox.IsEnabled = enable;
                    // restoreOriginalReplayNamesCheckBox.IsEnabled = enable;
                    return;
                case ReplayAction.Rename:
                    parseReplaysButton.IsEnabled = enable;
                    cancelParsingButton.IsEnabled = enable;
                    executeSortButton.IsEnabled = enable;
                    cancelSortButton.IsEnabled = enable;
                    undoRenamingButton.IsEnabled = enable;
                    // restoreOriginalReplayNamesCheckBox.IsEnabled = enable;
                    return;
                default:
                    return;
            }
        }

        private void SelectMapFolder(TextBox textbox)
        {
            var folderDialog = new CommonOpenFileDialog();
            folderDialog.IsFolderPicker = true;
            if (folderDialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                textbox.Text = folderDialog.FileName;
            }
        }

        private void OpenInFileExplorer(string filePath)
        {
            if (!File.Exists(filePath))
            {
                return;
            }

            string argument = "/select, \"" + filePath + "\"";
            try
            {
                Process.Start("explorer.exe", argument);
            }
            catch (Exception)
            {
                MessageBox.Show("An error occurred while trying to open the file.", "Failed to open file.", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region sorting

        private void ListBoxItem_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
        }

        private void ListBoxItem_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is ListBoxItem listBoxItem)
            {
                var listBox = listBoxItem.Parent as ListBox;
                ListBox.SetIsSelected(listBoxItem, !listBoxItem.IsSelected);
            }
        }

        private void sortCriteriaListBoxItem_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                if (sender is ListBoxItem)
                {
                    var draggedItem = sender as ListBoxItem;
                    if (draggedItem == null)
                        return;

                    _isDragging = true;
                    if (DragDrop.DoDragDrop(draggedItem, new Tuple<ListBoxItem, bool>(draggedItem, draggedItem.IsSelected), DragDropEffects.Move) == DragDropEffects.None)
                        _isDragging = false;
                }
            }
        }

        private void sortCriteriaListBoxItem_Drop(object sender, DragEventArgs e)
        {
            _isDragging = false;
            var source = e.Data.GetData(typeof(Tuple<ListBoxItem, bool>)) as Tuple<ListBoxItem, bool>;
            var target = sender as ListBoxItem;

            if (source == null || target == null)
                return;

            var targetIndex = sortCriteriaListBox.ItemContainerGenerator.IndexFromContainer(target);

            if (targetIndex < 0)
                return;

            sortCriteriaListBox.Items.Remove(source.Item1);
            sortCriteriaListBox.Items.Insert(targetIndex, source.Item1);
            (sortCriteriaListBox.Items.GetItemAt(targetIndex) as ListBoxItem).IsSelected = source.Item2;
            sortCriteriaListBox.Items.Refresh();
        }

        private void SortCriteriaListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isDragging && sender is ListBox)
            {
                var listBox = sender as ListBox;
                UpdateSortCriteriaParametersControls(e.AddedItems.Cast<ListBoxItem>().FirstOrDefault()?.Content as string, e.RemovedItems.Cast<ListBoxItem>().FirstOrDefault()?.Content as string);
            }
        }

        private void UpdateSortCriteriaParametersControls(string selectedItem, string unselectedItem)
        {
            var PanelToAdd = GetSortCriteriaParametersPanel(selectedItem);
            if (PanelToAdd != null)
            {
                sortCriteriaParameters.Children.Add(PanelToAdd);
            }
            var PanelToRemove = GetPanelWithName(sortCriteriaParameters, unselectedItem);
            if (PanelToRemove != null)
            {
                sortCriteriaParameters.Children.Remove(PanelToRemove);
            }
        }

        private Panel GetSortCriteriaParametersPanel(string selectedItem)
        {
            switch (selectedItem)
            {
                case "PLAYERNAME":
                    StackPanel playername = new StackPanel();
                    playername.Name = "PLAYERNAME";
                    playername.Orientation = Orientation.Vertical;
                    RadioButton winner = new RadioButton();
                    winner.Name = "Winner";
                    winner.Content = "Winner";
                    winner.GroupName = "MakeFolderFor";
                    RadioButton loser = new RadioButton();
                    loser.Name = "Loser";
                    loser.Content = "Loser";
                    loser.GroupName = "MakeFolderFor";
                    RadioButton both = new RadioButton();
                    both.Name = "Both";
                    both.Content = "Both";
                    both.GroupName = "MakeFolderFor";
                    RadioButton none = new RadioButton();
                    none.Name = "None";
                    none.Content = "None";
                    none.GroupName = "MakeFolderFor";
                    playername.Children.Add(winner);
                    playername.Children.Add(loser);
                    playername.Children.Add(both);
                    playername.Children.Add(none);
                    playername.Margin = new Thickness(0, 10, 10, 10);
                    return playername;
                case "DURATION":
                    StackPanel duration = new StackPanel();
                    duration.Name = "DURATION";
                    duration.Orientation = Orientation.Horizontal;
                    Label DurationIntervalsLabel = new Label();
                    DurationIntervalsLabel.Content = "Duration intervals: ";
                    TextBox DurationIntervalsTextBox = new TextBox();
                    DurationIntervalsTextBox.MinWidth = 200;
                    duration.Children.Add(DurationIntervalsLabel);
                    duration.Children.Add(DurationIntervalsTextBox);
                    duration.Margin = new Thickness(0, 10, 10, 10);
                    return duration;
                case "MATCHUP":
                    StackPanel gametypesPanel = new StackPanel();
                    gametypesPanel.Name = "MATCHUP";
                    gametypesPanel.Orientation = Orientation.Vertical;
                    CheckBox All = new CheckBox();
                    All.Content = "All";
                    All.Name = "All";
                    All.Click += All_Clicked;
                    gametypesPanel.Children.Add(All);
                    foreach (var aGametype in Enum.GetNames(typeof(Entities.GameType)))
                    {
                        CheckBox gametype = new CheckBox();
                        gametype.Name = aGametype;
                        gametype.Content = aGametype;
                        gametypesPanel.Children.Add(gametype);
                    }
                    gametypesPanel.Margin = new Thickness(0, 10, 10, 10);
                    return gametypesPanel;
                default:
                    return null;
            }
        }

        private void All_Clicked(object sender, RoutedEventArgs e)
        {
            var allCheckbox = sender as CheckBox;
            if (allCheckbox == null)
                return;

            if (!allCheckbox.IsChecked.HasValue)
                return;

            var gametypesPanel = allCheckbox.Parent as StackPanel;
            if (gametypesPanel == null)
                return;

            foreach (var child in gametypesPanel.Children)
            {
                var gametypeCheckbox = child as CheckBox;
                if (gametypeCheckbox != null && gametypeCheckbox.Name != "All")
                {
                    gametypeCheckbox.IsEnabled = allCheckbox.IsChecked.Value ? false : true;
                    gametypeCheckbox.IsChecked = allCheckbox.IsChecked.Value;
                }
            }
        }

        private Panel GetPanelWithName(Panel parent, string name)
        {
            if (parent != null)
            {
                foreach (var child in parent.Children)
                {
                    if ((child is Panel))
                    {
                        if ((child as Panel).Name == name)
                        {
                            return child as Panel;
                        }
                    }
                }
            }
            return null;
        }

        //TODO Rewrite this trash:
        // Validation doesn't need to stop immediately, validate all parameters etc and show error message with all invalid parameters if there are any
        // instantiate sorter at the end with all parameters
        private void executeSortButton_Click(object sender, RoutedEventArgs e)
        {
            if (_worker_ReplaySorter != null && _worker_ReplaySorter.IsBusy)
                return;

            string[] criteriaStringOrder = GetSortCriteria();

            if (criteriaStringOrder.Length == 0)
            {
                MessageBox.Show("Please make a selection of sort criteria. Not all of them can be none!", "No sort criteria selected!", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);
                return;
            }

            var filterReplays = filterReplaysCheckBox.IsChecked.HasValue && filterReplaysCheckBox.IsChecked.Value;
            if (filterReplays)
            {
                if (_filteredListReplays == null || _filteredListReplays.Count == 0)
                {
                    MessageBox.Show("Can not execute sort since filter did not return any replays!", "Failed to start sort: invalid filter", MessageBoxButton.OK, MessageBoxImage.Warning, MessageBoxResult.OK);
                    return;
                }
            }

            // get additional parameters
            SortCriteriaParameters SortCriteriaParameters;
            bool? makefolderforwinner = null;
            bool? makefolderforloser = null;
            _keepOriginalReplayNames = (bool)keepOriginalReplayNamesCheckBox.IsChecked;
            CustomReplayFormat customReplayFormat = null;

            if (!_keepOriginalReplayNames)
            {
                var customFormatTextBox = GetPanelWithName(keepOriginalReplayNamesPanel, "keepOriginalReplayNamesPanelPanel").Children.OfType<TextBox>().FirstOrDefault();
                try
                {
                    if (customFormatTextBox != null)
                    {
                        customReplayFormat = CustomReplayFormat.Create(customFormatTextBox.Text, filterReplays ? _filteredListReplays.Count : _listReplays.Count, true);
                    }
                    else
                    {
                        MessageBox.Show("Could not find the textbox containing the custom replay format.", "Error finding textbox", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);
                        return;
                    }
                }
                catch (ArgumentException)
                {
                    MessageBox.Show("Invalid custom replay format. Check help section for correct syntax", "Invalid syntax", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);
                    return;
                }
            }
            IDictionary<Entities.GameType, bool> validgametypes = null;
            int[] durations = null;
            foreach (var chosencriteria in criteriaStringOrder)
            {
                var chosenCriteriaPanel = GetPanelWithName(sortCriteriaParameters, chosencriteria);

                // how can i extract the information i want in a general way instead of needing to hardcode this

                if (chosencriteria == "PLAYERNAME")
                {
                    var checkedButton = chosenCriteriaPanel.Children.OfType<RadioButton>().Where(r => r.IsChecked == true).FirstOrDefault();
                    if (checkedButton == null)
                    {
                        MessageBox.Show("No playername folder creation option selected! Please make a selection between winner, loser, both or none!", "Failed to start sort: criteria error", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);
                        return;
                    }
                    switch (checkedButton.Name)
                    {
                        case "Winner":
                            makefolderforwinner = true;
                            makefolderforloser = false;
                            break;
                        case "Loser":
                            makefolderforloser = true;
                            makefolderforwinner = false;
                            break;
                        case "Both":
                            makefolderforwinner = true;
                            makefolderforloser = true;
                            break;
                        case "None":
                        default:
                            makefolderforwinner = false;
                            makefolderforloser = false;
                            break;
                    }
                }
                else if (chosencriteria == "MATCHUP")
                {
                    validgametypes = new Dictionary<Entities.GameType, bool>();
                    foreach (var checkbox in chosenCriteriaPanel.Children.OfType<CheckBox>())
                    {
                        if (checkbox.Name == "All")
                        {
                            if (checkbox.IsChecked == true)
                            {
                                foreach (var gametype in Enum.GetValues(typeof(Entities.GameType)))
                                {
                                    validgametypes[(Entities.GameType)gametype] = true;
                                }
                                break;
                            }
                        }
                        else
                        {
                            if (checkbox.IsChecked == true)
                                validgametypes[(Entities.GameType)Enum.Parse(typeof(Entities.GameType), checkbox.Name)] = true;
                            else
                                validgametypes[(Entities.GameType)Enum.Parse(typeof(Entities.GameType), checkbox.Name)] = false;
                        }
                    }
                    if (!validgametypes.Values.Contains(true))
                    {
                        MessageBox.Show("Please select at least one game type to include in the sort!", "Failed to start sort: criteria error", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);
                        return;
                    }
                }
                else if (chosencriteria == "DURATION")
                {
                    var durationsTextBoxText = chosenCriteriaPanel.Children.OfType<TextBox>().First().Text;
                    string[] durationsStrings = durationsTextBoxText.Split(' ');
                    int NumberOfDurations = durationsStrings.Length;
                    durations = new int[NumberOfDurations];

                    int index = 0;
                    bool ParseSuccess = durationsStrings.All(x => int.TryParse(x, out durations[index++]));
                    if (!ParseSuccess || durations.Contains(0))
                    {
                        MessageBox.Show("Give a space separated list of (non-zero) integer values you want to use as upper boundaries for duration intervals (in minutes). For example \"10 20 30\" this will result in the following intervals: 0-10, 10-20, 20-30, 30++", "Invalid interval input", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);
                        return;
                    }
                    durations = durations.OrderBy(x => x).ToArray();
                }
            }

            // only if directory exists
            if (Directory.Exists(sortOutputDirectoryTextBox.Text))
            {
                if (filterReplays)
                {
                    _sorter = new Sorter(sortOutputDirectoryTextBox.Text, _filteredListReplays);
                }
                else
                {
                    _sorter = new Sorter(sortOutputDirectoryTextBox.Text, _listReplays);
                }
            }
            else
            {
                MessageBox.Show(string.Format("Could not find directory {0}", sortOutputDirectoryTextBox.Text), "Failed to start sort: directory error", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);
                return;
            }

            SortCriteriaParameters = new SortCriteriaParameters(makefolderforwinner, makefolderforloser, validgametypes, durations);
            _sorter.SortCriteriaParameters = SortCriteriaParameters;

            if (criteriaStringOrder.Length > 1)
            {
                _sorter.SortCriteria = (Criteria)Enum.Parse(typeof(Criteria), string.Join(",", criteriaStringOrder));
            }
            else
            {
                _sorter.SortCriteria = (Criteria)Enum.Parse(typeof(Criteria), criteriaStringOrder[0]);
            }
            //TODO rewrite this crap so you instantiate the sorter in the end with all parameters. Then you can make OriginalCriteriaStringOrder readonly
            _sorter.CriteriaStringOrder = criteriaStringOrder;
            _sorter.CriteriaStringOrder = criteriaStringOrder;

            if (customReplayFormat != null)
                _sorter.CustomReplayFormat = customReplayFormat;

            var isPreview = sortIsPreview.IsChecked.HasValue && sortIsPreview.IsChecked.Value;
            if (isPreview)
            {
                _previewSortArguments = Tuple.Create(criteriaStringOrder, SortCriteriaParameters, customReplayFormat, _sorter.ListReplays);
            }

            _sorter.GenerateIntermediateFolders = _replaySorterConfiguration.GenerateIntermediateFoldersDuringSorting;

            if (_worker_ReplaySorter == null)
            {
                _worker_ReplaySorter = new BackgroundWorker();
                _worker_ReplaySorter.WorkerReportsProgress = true;
                _worker_ReplaySorter.WorkerSupportsCancellation = true;
                _worker_ReplaySorter.DoWork += worker_SortReplays;
                _worker_ReplaySorter.ProgressChanged += worker_ProgressChangedSortingReplays;
                _worker_ReplaySorter.RunWorkerCompleted += worker_SortingReplaysCompleted;
            }
            _swSort.Start();
            _worker_ReplaySorter.RunWorkerAsync(isPreview);
        }

        private string[] GetSortCriteria()
        {
            return sortCriteriaListBox.SelectedItems.Cast<ListBoxItem>().OrderBy(l => sortCriteriaListBox.Items.IndexOf(l)).Select(l => l.Content.ToString().ToUpper()).ToArray();
        }

        private void worker_SortReplays(object sender, DoWorkEventArgs e)
        {
            var SorterConditions = CheckSorterConditions(_sorter);

            if (SorterConditions.GoodToGo == true)
            {
                _sortingReplays = true;
                var isPreview = (bool)e.Argument;
                if (isPreview)
                {
                    var tree = _sorter.PreviewSort(_keepOriginalReplayNames, _worker_ReplaySorter, _replaysThrowingExceptions);
                    _previewTree = tree;
                    e.Result = tree;
                }
                else
                {
                    // TODO you need to verify the sort criteria parameters of preview are the exact same as those used now ( sortcriteria, sortcriteriaparameters, replays )
                    if (_previewTree != null && _sorter.MatchesInput(_previewTree, _previewSortArguments))
                    {
                        e.Result = _sorter.ExecuteSortAsync(_previewTree, _worker_ReplaySorter, _replaysThrowingExceptions);
                        _previewTree = null;
                        _previewSortArguments = null;
                    }
                    else
                    {
                        e.Result = _sorter.ExecuteSortAsync(_keepOriginalReplayNames, _worker_ReplaySorter, _replaysThrowingExceptions);
                    }
                }
                ReplayHandler.LogBadReplays(_replaysThrowingExceptions, _replaySorterConfiguration.LogDirectory, $"{DateTime.Now} - Error while {(isPreview ? "previewing " : string.Empty)}sorting replay: {{0}} with arguments {_sorter.ToString()}");

                if (_worker_ReplaySorter.CancellationPending == true)
                {
                    e.Cancel = true;
                    return;
                }
                // e.Result = result of sorter.ExecuteSortAsync
                // implement sorter.ExecuteSortAsync
                // how to report progress in this async task??
                // how to CANCEL the tasks??? I'm literally returning NULLS inside my methods and having to return everywhere, can't access DoWorkEventArgs unless I pass it, which I didn't!!
            }
            else
            {
                e.Cancel = true;
                _boolAnswer = SorterConditions;
                //e.Result = SorterConditions;
                return;
            }
        }

        private void worker_ProgressChangedSortingReplays(object sender, ProgressChangedEventArgs e)
        {
            if (e.UserState == null)
            {
                progressBarSortingReplays.Value = e.ProgressPercentage;
                if (_errorMessage != string.Empty)
                {
                    statusBarErrors.Content = _errorMessage;
                }
                if (_sortingReplays)
                {
                    statusBarAction.Content = "Sorting replays...";
                }
            }
            else
            {
                progressBarSortingReplays.Value = e.ProgressPercentage;
                statusBarAction.Content = (string)e.UserState;
            }
        }

        private void worker_SortingReplaysCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            _swSort.Stop();
            if (e.Cancelled == true)
            {
                statusBarAction.Content = "Sorting cancelled...";
                progressBarSortingReplays.Value = 0;

                if (_boolAnswer != null)
                {
                    MessageBox.Show(_boolAnswer.Message, "Failed to start sort", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);
                }
                else
                {
                    MessageBox.Show("Sorting cancelled...", "Cancelled", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK);
                }
                ResetReplaySortingVariables();
            }
            else
            {
                statusBarAction.Content = "Finished sorting replays";
                MessageBox.Show(string.Format("Finished sorting replays! It took {0} to sort {1} replays. {2} replays encountered exceptions.", _swSort.Elapsed, _listReplays.Count, _replaysThrowingExceptions.Count()), "Finished Sorting", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK);
                ResetReplaySortingVariables();
                _replaysThrowingExceptions.Clear();
                if (e.Error != null)
                {
                    MessageBox.Show("Something went wrong during sorting.", "Failed to sort!", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);
                    ErrorLogger.GetInstance()?.LogError($"{DateTime.Now} - Failed to execute sort: {e.Error.Message}", ex: e.Error.InnerException);
                    return;
                }
                var root = (e.Result as DirectoryFileTree).Root;
                Mouse.OverrideCursor = Cursors.Wait;
                sortOutputTreeView.ItemsSource = root;
                Dispatcher.Invoke(() => Mouse.OverrideCursor = null, System.Windows.Threading.DispatcherPriority.ContextIdle);
            }
        }

        private void ResetReplaySortingVariables()
        {
            // not implemented
            _sortingReplays = false;
            // KeepOriginalReplayNames = true;
            _boolAnswer = null;
            _swSort.Reset();
        }

        private BoolAnswer CheckSorterConditions(Sorter aSorter)
        {
            if (aSorter.ListReplays == null || aSorter.ListReplays.Count() == 0)
            {
                return new BoolAnswer("You have to parse replays before you can sort. File list is empty!", false);
            }
            if (!Directory.Exists(aSorter.CurrentDirectory))
            {
                return new BoolAnswer("Output directory does not exist!", false);
            }
            return new BoolAnswer("", true);
        }

        private void setSortOutputDirectoryMenuItem_Click(object sender, RoutedEventArgs e)
        {
            SelectMapFolder(sortOutputDirectoryTextBox);
        }

        private void sortOutputDirectoryButton_Click(object sender, RoutedEventArgs e)
        {
            SelectMapFolder(sortOutputDirectoryTextBox);
        }

        private void cancelSortButton_Click(object sender, RoutedEventArgs e)
        {
            if (_worker_ReplaySorter != null && _worker_ReplaySorter.IsBusy)
            {
                _worker_ReplaySorter.CancelAsync();
            }
        }

        private void keepOriginalReplayNamesCheckBox_Click(object sender, RoutedEventArgs e)
        {
            var checkbox = sender as CheckBox;
            if (checkbox.IsChecked == false)
            {
                StackPanel KeepOriginalReplayNames = new StackPanel();
                KeepOriginalReplayNames.Name = "keepOriginalReplayNamesPanelPanel";
                KeepOriginalReplayNames.Orientation = Orientation.Horizontal;
                Label CustomFormatLabel = new Label();
                CustomFormatLabel.Content = "Custom format: ";
                TextBox CustomFormatTextBox = new TextBox();
                CustomFormatTextBox.MinWidth = 300;
                KeepOriginalReplayNames.Children.Add(CustomFormatLabel);
                KeepOriginalReplayNames.Children.Add(CustomFormatTextBox);
                keepOriginalReplayNamesPanel.Children.Add(KeepOriginalReplayNames);
            }
            else
            {
                var PanelToRemove = GetPanelWithName(keepOriginalReplayNamesPanel, "keepOriginalReplayNamesPanelPanel");
                keepOriginalReplayNamesPanel.Children.Remove(PanelToRemove);
            }
        }

        #endregion

        #region renaming

        private void renameInPlaceCheckBox_Click(object sender, RoutedEventArgs e)
        {
            var renameInPlaceCheckBoxIsEnabled = renameInPlaceCheckBox.IsChecked.HasValue && renameInPlaceCheckBox.IsChecked.Value;
            restoreOriginalReplayNamesCheckBox.IsEnabled = !renameInPlaceCheckBoxIsEnabled;
            replayRenamingOutputDirectoryButton.IsEnabled = !(renameInPlaceCheckBoxIsEnabled);
            replayRenamingOutputDirectoryTextBox.IsEnabled = !(renameInPlaceCheckBoxIsEnabled);
        }

        private void replayRenamingOutputDirectoryButton_Click(object sender, RoutedEventArgs e)
        {
            SelectMapFolder(replayRenamingOutputDirectoryTextBox);
        }

        //TODO rewrite this trash...
        // Validate everything, return error message with all validation if necessary
        private void executeRenamingButton_Click(object sender, RoutedEventArgs e)
        {
            if (_worker_ReplayRenamer != null && _worker_ReplayRenamer.IsBusy)
                return;

            bool? renameInPlace = renameInPlaceCheckBox.IsChecked;
            bool? restoreOriginalReplayNames = restoreOriginalReplayNamesCheckBox.IsChecked;
            bool? isPreview = renameIsPreviewCheckBox.IsChecked;
            string replayRenamingSyntax = replayRenamingSyntaxTextBox.Text;
            string replayRenamingOutputDirectory = replayRenamingOutputDirectoryTextBox.Text;

            var renameInPlaceValue = renameInPlace.HasValue && renameInPlace.Value;
            var restoreOriginalReplayNamesValue = restoreOriginalReplayNames.HasValue && restoreOriginalReplayNames.Value;
            var isPreviewValue = isPreview.HasValue && isPreview.Value;
            var filterReplays = filterReplaysCheckBox.IsChecked.HasValue && filterReplaysCheckBox.IsChecked.Value;
            if (filterReplays)
            {
                if (_filteredListReplays == null)
                {
                    MessageBox.Show("Can not execute rename since filter did not return any replays!", "Failed to start rename: invalid filter", MessageBoxButton.OK, MessageBoxImage.Warning, MessageBoxResult.OK);
                    return;
                }
            }

            CustomReplayFormat customReplayFormat = null;

            if (!restoreOriginalReplayNamesValue)
            {
                if (string.IsNullOrEmpty(replayRenamingSyntax))
                {
                    MessageBox.Show("Please specify a custom replay format.", "Empty replay format", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);
                    return;
                }

                try
                {
                    customReplayFormat = CustomReplayFormat.Create(replayRenamingSyntax, filterReplays ? _filteredListReplays.Count : _listReplays.Count, true);
                }
                catch (ArgumentException)
                {
                    MessageBox.Show("Invalid custom replay format. Check help section for correct syntax", "Invalid syntax", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);
                    return;
                }
            }
            else
            {
                if (_listReplays.All(r => r.FilePath == r.OriginalFilePath))
                {
                    MessageBox.Show("Replays still have their original file names. Restore is not necessary.", "Unnecessary restore", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK);
                    return;
                }
            }

            if (renameInPlaceValue || restoreOriginalReplayNamesValue)
            {
                replayRenamingOutputDirectory = string.Empty;
            }

            var renamingParameters = RenamingParameters.Create(customReplayFormat, replayRenamingOutputDirectory, renameInPlaceValue, restoreOriginalReplayNamesValue, isPreviewValue);

            if (renamingParameters == null)
            {
                MessageBox.Show("Please fill in a proper renaming format and an output directory, or tick off one of the checkboxes.", "Invalid parameters", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);
                return;
            }

            Renamer replayRenamer = null;

            if (filterReplays)
            {
                replayRenamer = new Renamer(renamingParameters, _filteredListReplays);
            }
            else
            {
                replayRenamer = new Renamer(renamingParameters, _listReplays);
            }

            statusBarAction.Content = "Renaming replays...";

            if (_worker_ReplayRenamer == null)
            {
                _worker_ReplayRenamer = new BackgroundWorker();
                _worker_ReplayRenamer.WorkerReportsProgress = true;
                _worker_ReplayRenamer.WorkerSupportsCancellation = true;
                _worker_ReplayRenamer.DoWork += worker_RenameReplays;
                _worker_ReplayRenamer.ProgressChanged += worker_ProgressChangedRenamingReplays;
                _worker_ReplayRenamer.RunWorkerCompleted += worker_RenamingReplaysCompleted;
            }
            _activeWorker = _worker_ReplayRenamer;
            _worker_ReplayRenamer.RunWorkerAsync(replayRenamer);
        }

        private void worker_RenameReplays(object sender, DoWorkEventArgs e)
        {
            var replayRenamer = e.Argument as Renamer;
            // var renameInPlace = replayRenamer.RenameInPlace;
            // var restoreOriginalReplayNames = replayRenamer.RestoreOriginalReplayNames;
            // var isPreview = replayRenamer.IsPreview;
            // ServiceResult<ServiceResultSummary<IEnumerable<File<IReplay>>>> response = null;
            _renamingReplays = true;

            //TODO wtf... why do you have these different methods RenameInPlace and RestoreOriginalNames when the RenamingParameters used to create the Renamer already give you the necessaryinformation ???
            // if (renameInPlace)
            // {
            //     response = replayRenamer.RenameInPlaceAsync(sender as BackgroundWorker);
            // }
            // else if (restoreOriginalReplayNames)
            // {
            //     response = replayRenamer.RestoreOriginalNames(sender as BackgroundWorker);
            // }
            // else
            // {
            //     // renaming into another directory
            //     _renamingToOutputDirectory = true;
            //     response = replayRenamer.RenameToDirectoryAsync(sender as BackgroundWorker);
            // }
            _renamingToOutputDirectory = !string.IsNullOrEmpty(replayRenamer.OutputDirectory);
            _renamingIsPreview = replayRenamer.IsPreview;
            var response = replayRenamer.RenameAsync(sender as BackgroundWorker);

            ReplayHandler.LogBadReplays(_replaysThrowingExceptions, _replaySorterConfiguration.LogDirectory, $"{DateTime.Now} - Error while renaming replay: {{0}} using arguments: {replayRenamer.ToString()}");
            e.Result = response;
        }

        private void worker_ProgressChangedRenamingReplays(object sender, ProgressChangedEventArgs e)
        {
            if (e.UserState == null)
            {
                progressBarRenamingOrRestoringReplays.Value = e.ProgressPercentage;
                if (_errorMessage != string.Empty)
                {
                    statusBarErrors.Content = _errorMessage;
                }
                if (_renamingReplays)
                {
                    statusBarAction.Content = "Renaming replays...";
                }
            }
            else
            {
                progressBarRenamingOrRestoringReplays.Value = e.ProgressPercentage;
                statusBarAction.Content = (string)e.UserState;
            }
        }

        private void worker_RenamingReplaysCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            _activeWorker = null;
            var response = e.Result as ServiceResult<ServiceResultSummary<IEnumerable<File<IReplay>>>>;
            progressBarSortingReplays.Value = 0;

            renameTransformationResultListView.ItemsSource = RenderRenaming(response.Result.Result);

            if (_renamingToOutputDirectory || _renamingIsPreview)
            {
                // remove history
                foreach (var replay in response.Result.Result)
                {
                    replay.Rewind();
                    replay.RemoveAfterCurrent();
                }
            }
            else
            {
                AddUndoable(response.Result.Result);
            }

            if (e.Cancelled == true)
            {
                statusBarAction.Content = "Renaming cancelled...";
                MessageBox.Show(response.Result.Message, "Renaming cancelled", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK);
            }
            else
            {
                statusBarAction.Content = "Finished renaming replays";
                // ??
                if (!response.Success)
                {
                    MessageBox.Show(string.Join(". ", response.Errors), "Invalid rename", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);
                }
                else
                {
                    MessageBox.Show(response.Result.Message, "Finished renaming", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK);
                }
            }
            _renamingToOutputDirectory = false;
            _renamingReplays = false;
            _renamingIsPreview = false;
            listViewReplays.Items.Refresh();
        }

        private void AddUndoable(IEnumerable<File<IReplay>> replays)
        {
            if (replays == null || replays.Count() == 0 || _replaySorterConfiguration.MaxUndoLevel == 0)
                return;

            _renamedReplaysList = _renamedReplaysList ?? new LinkedList<IEnumerable<File<IReplay>>>();

            if (_renamedReplayListHead == null)
            {
                _renamedReplaysList.Clear();
                _renamedReplaysList.AddFirst(replays);
                _renamedReplayListHead = _renamedReplaysList.First;
            }
            else
            {
                while (_renamedReplayListHead.Next != null)
                {
                    _renamedReplaysList.Remove(_renamedReplayListHead.Next);
                }
                _renamedReplaysList.AddAfter(_renamedReplayListHead, replays);
                _renamedReplayListHead = _renamedReplayListHead.Next;
            }

            while (_renamedReplaysList.Count > _replaySorterConfiguration.MaxUndoLevel)
            {
                _renamedReplaysList.RemoveFirst();
            }
        }

        private IEnumerable<ReplayRenamer.Renaming> RenderRenaming(IEnumerable<File<IReplay>> result)
        {
            var renamings = new List<ReplayRenamer.Renaming>();

            foreach (var replay in result)
            {
                var newName = replay.FilePath;
                replay.Rewind();
                var oldName = replay.FilePath;
                replay.Forward();
                renamings.Add(new ReplayRenamer.Renaming(replay, oldName, newName));
            }

            return renamings;
        }

        private void ChangeRenameTransformationListBoxRendering_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var currentPathResource = button.Tag.ToString();
            string toApplyPathResouce = string.Empty;
            switch (currentPathResource)
            {
                case "short-path":
                    toApplyPathResouce = "long-path";
                    break;
                case "long-path":
                    toApplyPathResouce = "short-path";
                    break;
                default: throw new Exception();
            }
            button.Tag = toApplyPathResouce;
            var pathResource = (button.Parent as StackPanel).FindResource(toApplyPathResouce);
            button.Content = pathResource;
        }

        private void cancelRenamingButton_Click(object sender, RoutedEventArgs e)
        {
            if (_activeWorker != null && _activeWorker.IsBusy)
            {
                _activeWorker.CancelAsync();
            }
        }

        // undo look at head
        private void undoRenamingButton_Click(object sender, RoutedEventArgs e)
        {
            var isRenamed = _renamedReplaysList?.Count > 0 && _renamedReplayListHead != null;

            if (!isRenamed)
            {
                MessageBox.Show("Please execute a rename in place before attempting to undo one.", "Invalid undo action", MessageBoxButton.OK, MessageBoxImage.Warning, MessageBoxResult.OK);
                return;
            }

            if (_worker_Undoer == null)
            {
                _worker_Undoer = new BackgroundWorker();
                _worker_Undoer.WorkerReportsProgress = true;
                _worker_Undoer.WorkerSupportsCancellation = true;
                _worker_Undoer.DoWork += worker_UndoRename;
                _worker_Undoer.ProgressChanged += worker_ProgressChangedUndoRename;
                _worker_Undoer.RunWorkerCompleted += worker_UndoRenamingCompleted;
            }
            _activeWorker = _worker_Undoer;
            _worker_Undoer.RunWorkerAsync(true);
        }

        // redo look at head.next
        private void redoRenamingButton_Click(object sender, RoutedEventArgs e)
        {
            //var canRedo = _renamedReplayListHead != _renamedReplaysList.Last;
            var canRedo = _renamedReplaysList?.Count > 0 && (_renamedReplayListHead == null || _renamedReplayListHead.Next != null);

            if (!canRedo)
            {
                MessageBox.Show("Please execute an undo rename in place before attempting to redo one.", "Invalid redo action", MessageBoxButton.OK, MessageBoxImage.Warning, MessageBoxResult.OK);
                return;
            }

            if (_worker_Undoer == null)
            {
                _worker_Undoer = new BackgroundWorker();
                _worker_Undoer.WorkerReportsProgress = true;
                _worker_Undoer.WorkerSupportsCancellation = true;
                _worker_Undoer.DoWork += worker_UndoRename;
                _worker_Undoer.ProgressChanged += worker_ProgressChangedUndoRename;
                _worker_Undoer.RunWorkerCompleted += worker_UndoRenamingCompleted;
            }
            _activeWorker = _worker_Undoer;
            _worker_Undoer.RunWorkerAsync(false);
        }

        private void worker_UndoRename(object sender, DoWorkEventArgs e)
        {
            if ((bool)e.Argument == true)
            {
                // undo
                _undoingRename = true;

                var replayRenamer = new Renamer(RenamingParameters.Default, _renamedReplayListHead.Value);
                e.Result = replayRenamer.UndoRename(sender as BackgroundWorker);

                _renamedReplayListHead = _renamedReplayListHead.Previous;
            }
            else
            {
                // redo
                _renamedReplayListHead = _renamedReplayListHead?.Next ?? _renamedReplaysList.First;

                var replayRenamer = new Renamer(RenamingParameters.Default, _renamedReplayListHead.Value);
                e.Result = replayRenamer.RedoRename(sender as BackgroundWorker);
            }
        }

        private void worker_ProgressChangedUndoRename(object sender, ProgressChangedEventArgs e)
        {
            if (e.UserState == null)
            {
                progressBarRenamingOrRestoringReplays.Value = e.ProgressPercentage;
                if (_errorMessage != string.Empty)
                {
                    statusBarErrors.Content = _errorMessage;
                }
                if (_undoingRename)
                {
                    statusBarAction.Content = "Undoing last renaming of replays...";
                }
                else
                {
                    statusBarAction.Content = "Redoing last renaming of replays...";
                }
            }
            else
            {
                progressBarRenamingOrRestoringReplays.Value = e.ProgressPercentage;
                statusBarAction.Content = (string)e.UserState;
            }
        }

        private void worker_UndoRenamingCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            _activeWorker = null;
            var response = e.Result as ServiceResult<ServiceResultSummary<IEnumerable<File<IReplay>>>>;
            progressBarRenamingOrRestoringReplays.Value = 0;

            //TODO doesn't work
            // renameTransformationResultListView.ItemsSource = RenderRenaming(response.Result.Result);
            renameTransformationResultListView.ItemsSource = RenderUndoOrRedo(response.Result.Result, _undoingRename);

            if (e.Cancelled)
            {
                statusBarAction.Content = _undoingRename ? "Undoing renaming cancelled..." : "Redoing renaming cancelled...";
                MessageBox.Show(response.Result.Message, _undoingRename ? "Undoing renaming cancelled" : "Redoing renaming cancelled", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK);
            }
            else
            {
                statusBarAction.Content = _undoingRename ? "Finished undoing last renaming." : "Finished redoing last renaming";

                if (response.Success)
                {
                    MessageBox.Show(response.Result.Message, _undoingRename ? "Finished undoing last renaming" : "Finished redoing last renaming", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK);
                }
                else
                {
                    MessageBox.Show(string.Join(". ", response.Errors), _undoingRename ? "Failed undoing last renaming" : "Failed redoing last renaming", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);
                }
            }
            _undoingRename = false;
            listViewReplays.Items.Refresh();
        }

        private IEnumerable<ReplayRenamer.Renaming> RenderUndoOrRedo(IEnumerable<File<IReplay>> replays, bool isUndo)
        {
            var renamings = new List<ReplayRenamer.Renaming>();

            if (isUndo)
            {
                foreach (var replay in replays)
                {
                    var newName = replay.FilePath;
                    replay.Forward();
                    var oldName = replay.FilePath;
                    replay.Rewind();
                    renamings.Add(new ReplayRenamer.Renaming(replay, oldName, newName));
                }
            }
            else
            {
                foreach (var replay in replays)
                {
                    var newName = replay.FilePath;
                    replay.Rewind();
                    var oldName = replay.FilePath;
                    replay.Forward();
                    renamings.Add(new ReplayRenamer.Renaming(replay, oldName, newName));
                }
            }
            return renamings.AsEnumerable();
        }

        private void RestoreOriginalReplayNamesCheckBox_Click(object sender, RoutedEventArgs e)
        {
            var restoreIsChecked = (restoreOriginalReplayNamesCheckBox.IsChecked.HasValue && restoreOriginalReplayNamesCheckBox.IsChecked.Value);
            renameInPlaceCheckBox.IsEnabled = !restoreIsChecked;
            replayRenamingSyntaxTextBox.IsEnabled = !restoreIsChecked;
            replayRenamingOutputDirectoryButton.IsEnabled = !restoreIsChecked;
            replayRenamingOutputDirectoryTextBox.IsEnabled = !restoreIsChecked;
        }

        #endregion

        #region filtering


        private BackgroundWorker worker_replayFilterer;
        private ReplayFilterer _replayFilterer = new ReplayFilterer();
        private string _lastExecutedFilter;

        private void FilterReplaysTextBox_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                if (string.IsNullOrWhiteSpace(filterReplaysTextBox.Text))
                {
                    if (listViewReplays.ItemsSource != _listReplays)
                    {
                        listViewReplays.ItemsSource = _listReplays;
                        filterReplaysProgressBar.Value = 0;
                        statusBarAction.Content = string.Empty;
                    }

                    return;
                }

                if (filterReplaysTextBox.Text == _lastExecutedFilter)
                {
                    listViewReplays.ItemsSource = _filteredListReplays;
                    return;
                }

                if (worker_replayFilterer == null)
                {
                    worker_replayFilterer = new BackgroundWorker();
                    worker_replayFilterer.WorkerReportsProgress = true;
                    // worker_replayFilterer.WorkerSupportsCancellation = true;
                    worker_replayFilterer.DoWork += worker_FilterReplays;
                    worker_replayFilterer.ProgressChanged += worker_ProgressChangedFilteringReplays;
                    worker_replayFilterer.RunWorkerCompleted += worker_FilteringReplaysCompleted;
                }
                worker_replayFilterer.RunWorkerAsync(filterReplaysTextBox.Text);
            }
        }

        private void worker_FilterReplays(object sender, DoWorkEventArgs e)
        {
            var filterExpression = e.Argument as string;
            if (string.IsNullOrWhiteSpace(filterExpression))
                return;

            _filteredListReplays = _replayFilterer.Apply(_listReplays, filterExpression, sender as BackgroundWorker);
            e.Result = filterExpression;
        }

        private void worker_ProgressChangedFilteringReplays(object sender, ProgressChangedEventArgs e)
        {
            filterReplaysProgressBar.Value = e.ProgressPercentage;
            if (e.UserState == null)
            {
                if (_errorMessage != string.Empty)
                {
                    statusBarErrors.Content = _errorMessage;
                }
            }
            else
            {
                statusBarAction.Content = (string)e.UserState;
            }
        }

        private void worker_FilteringReplaysCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (!e.Cancelled && e.Error == null)
            {
                listViewReplays.ItemsSource = _filteredListReplays;
                _lastExecutedFilter = e.Result as string;
                statusBarAction.Content = $"{_filteredListReplays?.Count ?? 0} replays matched filter.";
            }
        }

        private void ResetFiltering()
        {
            _filteredListReplays?.Clear();
            _lastExecutedFilter = null;
        }

        #endregion

        #region backups

        #region fields

        private BWContext _activeUow;
        private HashSet<string> _databaseNames = new HashSet<string>();
        private List<BackupWithCount> _backups = new List<BackupWithCount>();
        private static Regex _getSqliteFileName = new Regex(@"data source=(.*);", RegexOptions.IgnoreCase);

        #endregion

        private void SelectDatabaseDirectoryButton_Click(object sender, RoutedEventArgs e)
        {
            SelectMapFolder(databaseDirectoryTextBox);
        }

        private void CreateNewDatabaseButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!Directory.Exists(databaseDirectoryTextBox.Text))
                {
                    MessageBox.Show("Directory does not exist! Please select a valid directory to create a new database in.", "Nonexisting directory!", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);
                    return;
                }

                var databaseNameWithoutInvalidCharacters = FileHandler.RemoveInvalidChars(databaseNameTextBox.Text);

                if (databaseNameWithoutInvalidCharacters != databaseNameTextBox.Text)
                {
                    var invalidCharacters = databaseNameTextBox.Text.Except(databaseNameWithoutInvalidCharacters).Distinct();
                    // alternatively
                    // var invalidCharacters2 = from character in databaseNameTextBox.Text
                    //                         join otherCharacter in databaseNameWithoutInvalidCharacters on character equals otherCharacter into matchedCharacters
                    //                         where matchedCharacters == null
                    //                         select character;

                    MessageBox.Show($"Database name contains the following invalid characters: {string.Join(",", invalidCharacters)}. Please remove them and try again.", "Invalid characters in file name.", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);
                    return;
                }

                var databaseName = Path.Combine(databaseDirectoryTextBox.Text, databaseNameTextBox.Text);
                if (Path.GetExtension(databaseName).ToLower() != ".sqlite")
                {
                    databaseName = databaseName + ".sqlite";
                }

                if (File.Exists(databaseName))
                {
                    MessageBox.Show(_databaseNames.Contains(databaseName) ? 
                            "This database already exists and is part of the existing database list. Please choose it from the dropdown." : 
                            "Cannot create this database since it already exists! Please choose add existing database instead!", 
                        "Database already exists!", 
                        MessageBoxButton.OK, 
                        MessageBoxImage.Warning, 
                        MessageBoxResult.OK);
                    return;
                }

                _activeUow = BWContext.Create(databaseName);
                RememberDatabase(databaseName);
                ReloadDatabaseComboBox(databaseName);
                statusBarAction.Content = $"Database {databaseName} successfully created!";
            }
            catch (ArgumentException)
            {
                MessageBox.Show($"Database name cannot be an empty string", "Invalid database name", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);
            }
            catch (Exception ex)
            {
                ErrorLogger.GetInstance()?.LogError($"{DateTime.Now} - Something went wrong while creating the database.", ex: ex);
                MessageBox.Show($"Something went wrong while creating the database: {ex.Message}", "Failed to create database!", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);
            }
        }

        private void RememberDatabase(string databaseName)
        {
            var databaseNames = _replaySorterConfiguration.BWContextDatabaseNames;

            if (string.IsNullOrWhiteSpace(databaseNames))
            {
                _replaySorterConfiguration.BWContextDatabaseNames = databaseName;
            }
            else
            {
                var databaseNamesHashSet = databaseNames.Split('|').ToHashSet();
                if (!databaseNamesHashSet.Contains(databaseName))
                {
                    _replaySorterConfiguration.BWContextDatabaseNames = databaseNames + "|" + databaseName;
                }
            }
        }

        private void RemoveDatabase(string databaseName)
        {
            var databaseNames = _replaySorterConfiguration.BWContextDatabaseNames;

            if (string.IsNullOrWhiteSpace(databaseName))
                throw new InvalidOperationException("Cannot remove database from settings since settings are empty!");

            var databaseNamesToKeep = databaseNames.Split('|').Where(db =>
            {
                var match = _getSqliteFileName.Match(db);
                if (!match.Success)
                    return false;

                var dbName = match.Groups[1].Value;
                if (dbName.ToLower() == databaseName.ToLower())
                    return false;

                return true;
            });

            _replaySorterConfiguration.BWContextDatabaseNames = string.Join("|", databaseNamesToKeep);
        }

        private void ReloadDatabaseComboBox(string databaseName = "")
        {
            var databaseNames = _replaySorterConfiguration.BWContextDatabaseNames.Split('|').Where(db => !string.IsNullOrWhiteSpace(db)); 
            _databaseNames = databaseNames.ToHashSet();
            databaseComboBox.ItemsSource = _databaseNames;//.Select(db => Path.GetFileNameWithoutExtension(db));
            if (!string.IsNullOrWhiteSpace(databaseName))
            {
                databaseComboBox.SelectedItem = databaseName;
            }
        }

        private async void DatabaseComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                var databaseNameComboBox = sender as ComboBox;
                if (databaseNameComboBox.SelectedIndex == -1)
                    return;

                if (e.AddedItems.Count != 0)
                {
                    var databaseName = e.AddedItems[0] as string;
                    if (_activeUow != null)
                        _activeUow.Dispose();

                    _activeUow = BWContext.Create(databaseName, false);
                    var backups = _activeUow.BackupRepository.GetAll().ToList();
                    //TODO add caching??
                    _backups = (await Task.WhenAll(backups.Select(async b =>
                        new BackupWithCount
                        {
                            Id = b.Id,
                            Name = b.Name,
                            Comment = b.Comment,
                            RootDirectory = b.RootDirectory,
                            Date = b.Date,
                            Count = (await Task.Run(() => _activeUow.BackupRepository.GetNumberOfBackedUpReplays(b.Id).Value))
                        }
                    ))).ToList();
                    backupListView.ItemsSource = _backups;
                }
                else
                {
                    databaseNameComboBox.SelectedIndex = -1;
                    if (_activeUow != null)
                        _activeUow.Dispose();

                    _activeUow = null;
                }
            }
            catch (Exception ex)
            {
                ErrorLogger.GetInstance()?.LogError($"{DateTime.Now} - Something went wrong while opening the database.", ex: ex);
                MessageBox.Show($"Something went wrong while opening the database: {ex.Message}. Find the database and place it in the correct location, or clean the list to remove stale entries.", "Failed to open the database!", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);
            }
        }

        private void EmptyDatabaseButton_Click(object sender, RoutedEventArgs e)
        {
            if (_activeUow == null)
            {
                MessageBox.Show("Please choose a database from the list first!", "No database selected!", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);
                return;
            }

            var dbName = _activeUow.DatabaseName;
            var backupService = new BackupService(_activeUow);
            backupService.DeleteAllBackupsAndReplays();
            _activeUow = BWContext.Create(dbName);
        }

        private void DeleteDatabaseButton_Click(object sender, RoutedEventArgs e)
        {
            if (_activeUow == null)
            {
                MessageBox.Show("Please choose a database from the list first!", "No database selected!", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);
                return;
            }

            var databaseName = _activeUow.DatabaseName;

            if (!File.Exists(databaseName))
            {
                MessageBox.Show("Database file does not exist!", "Database not found!", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);
                return;
            }

            _activeUow.Dispose();
            _activeUow = null;
            try
            {
                File.Delete(databaseName);
                RemoveDatabase(databaseName);
                ReloadDatabaseComboBox();
            }
            catch (Exception ex)
            {
                ErrorLogger.GetInstance()?.LogError($"{DateTime.Now} - Something went wrong while attempting to delete the database {databaseName}: {ex.Message}", ex: ex);
                MessageBox.Show($"Something went wrong while attempting to delete the database {databaseName}: {ex.Message}", "Failed to delete database.", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);
            }
        }

        private void CleanDatabaseListButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var databasesToKeep = databaseComboBox.Items.Cast<string>().Where(db => File.Exists(db) || File.Exists(db + ".sqlite"));
                _replaySorterConfiguration.BWContextDatabaseNames = string.Join("|", databasesToKeep);
                ReloadDatabaseComboBox();
            }
            catch (Exception ex)
            {
                ErrorLogger.GetInstance()?.LogError($"{DateTime.Now} - Something went wrong while cleaning the database list: {ex.Message}", ex: ex);
                MessageBox.Show($"{DateTime.Now} - Something went wrong while cleaning the database list: {ex.Message}", "Failed to clean database list.", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);
            }
        }

        private void AddExistingDatabaseButton_Click(object sender, RoutedEventArgs e)
        {
            var databaseSelector = new CommonOpenFileDialog();
            databaseSelector.EnsureFileExists = true;
            databaseSelector.Multiselect = false;
            databaseSelector.Title = "Choose an existing sqlite database.";
            databaseSelector.Filters.Add(new CommonFileDialogFilter("sqlite database files", ".sqlite"));
            if (databaseSelector.ShowDialog() == CommonFileDialogResult.Ok)
            {
                RememberDatabase(databaseSelector.FileName);
                ReloadDatabaseComboBox(databaseSelector.FileName);
            }
        }

        private void CreateBackupButton_Click(object sender, RoutedEventArgs e)
        {
            if (_activeUow == null)
            {
                MessageBox.Show("Please select an existing database to operate on first!", "Invalid operation", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);
                return;
            }

            var dbName = _activeUow.DatabaseName;
            try
            {
                var createBackupDialog = new BackupWindow(BackupAction.Create, null, _activeUow);
                if (createBackupDialog.ShowDialog() == true)
                {
                    AddBackupAndRefresh(createBackupDialog.Backup);
                    //TODO ... I don't like this
                    _activeUow = BWContext.Create(dbName);
                };
            }
            catch (Exception ex)
            {
                ErrorLogger.GetInstance()?.LogError($"{DateTime.Now} - Something went wrong while creating a backup: {ex.Message}", ex: ex);
                MessageBox.Show($"{DateTime.Now} - Something went wrong while creating a backup: {ex.Message}", "Failed to create backup.", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);
            }
        }

        //TODO refactor
        private void AddBackupAndRefresh(BackupWithCount backupWithCount)
        {
            if (backupWithCount != null)
            {
                _backups.Add(backupWithCount);
                backupListView.Items.Refresh();
            }
        }

        private void InspectBackupButton_Click(object sender, RoutedEventArgs e)
        {
            if (_activeUow == null)
            {
                MessageBox.Show("Please select an existing database to operate on first!", "Invalid operation", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);
                return;
            }

            var backup = backupListView.SelectedItem as BackupWithCount;
            if (backup == null)
            {
                MessageBox.Show("Please select a backup from the list first!", "Invalid operation", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);
                return;
            }

            try
            {
                var inspectBackupDialog = new BackupWindow(BackupAction.Inspect, backup, _activeUow);
                inspectBackupDialog.ShowDialog();
            }
            catch (Exception ex)
            {
                ErrorLogger.GetInstance()?.LogError($"{DateTime.Now} - Something went wrong while inspecting a backup: {ex.Message}", ex: ex);
                MessageBox.Show($"{DateTime.Now} - Something went wrong while inspecting a backup: {ex.Message}", "Failed to inspect backup.", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);
            }
        }

        private void RestoreFromBackupButton_Click(object sender, RoutedEventArgs e)
        {
            if (_activeUow == null)
            {
                MessageBox.Show("Please select an existing database to operate on first!", "Invalid operation", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);
                return;
            }

            var backup = backupListView.SelectedItem as BackupWithCount;
            if (backup == null)
            {
                MessageBox.Show("Please select a backup from the list first!", "Invalid operation", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);
                return;
            }

            var dbName = _activeUow.DatabaseName;
            try
            {
                var restoreBackupDialog = new BackupWindow(BackupAction.Restore, backup, _activeUow);
                if (restoreBackupDialog.ShowDialog() == true)
                {
                    _activeUow = BWContext.Create(dbName);
                }
            }
            catch (Exception ex)
            {
                ErrorLogger.GetInstance()?.LogError($"{DateTime.Now} - Something went wrong while restoring a backup: {ex.Message}", ex: ex);
                MessageBox.Show($"{DateTime.Now} - Something went wrong while restoring a backup: {ex.Message}", "Failed to restore backup.", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);
            }
        }

        private void DeleteBackupButton_Click(object sender, RoutedEventArgs e)
        {
            if (_activeUow == null)
            {
                MessageBox.Show("Please select an existing database to operate on first!", "Invalid operation", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);
                return;
            }

            var backup = backupListView.SelectedItem as BackupWithCount;
            if (backup == null)
            {
                MessageBox.Show("Please select a backup from the list first!", "Invalid operation", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);
                return;
            }

            var dbName = _activeUow.DatabaseName;
            try
            {
                var deleteBackupDialog = new BackupWindow(BackupAction.Delete, backup, _activeUow);
                if (deleteBackupDialog.ShowDialog() == true)
                {
                    _backups.Remove(backup);
                    backupListView.Items.Refresh();
                    _activeUow = BWContext.Create(dbName);
                }
            }
            catch (Exception ex)
            {
                ErrorLogger.GetInstance()?.LogError($"{DateTime.Now} - Something went wrong while deleting a backup: {ex.Message}", ex: ex);
                MessageBox.Show($"{DateTime.Now} - Something went wrong while deleting a backup: {ex.Message}", "Failed to delete backup.", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);
            }
        }

        #endregion

        #region settings

        private void SetLoggingDirectoryMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var folderDialog = new CommonOpenFileDialog();
            folderDialog.IsFolderPicker = true;
            if (folderDialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                //TODO sanitize?
                _replaySorterConfiguration.LogDirectory = folderDialog.FileName;
            }
        }

        private void SetAdvancedSettingsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var advancedSettingsWindow = new AdvancedSettings(_replaySorterConfiguration);
            advancedSettingsWindow.ShowDialog();
        }

        #endregion

        #region sorting listview
        private GridViewColumnHeader _currentSortCol = null;
        private SortAdorner _sortAdorner = null;
        private void listViewReplaysColumnHeader_Click(object sender, RoutedEventArgs e)
        {
            GridViewColumnHeader column = (sender as GridViewColumnHeader);
            if (column == null)
                return;

            string sortBy = column.Tag.ToString();
            if (sortBy == "Players")
                return;

            if (_currentSortCol != null)
            {
                AdornerLayer.GetAdornerLayer(_currentSortCol).Remove(_sortAdorner);
                listViewReplays.Items.SortDescriptions.Clear();
            }

            ListSortDirection newDir = ListSortDirection.Ascending;
            if (_currentSortCol == column && _sortAdorner.Direction == newDir)
                newDir = ListSortDirection.Descending;

            _currentSortCol = column;
            _sortAdorner = new SortAdorner(_currentSortCol, newDir);
            AdornerLayer.GetAdornerLayer(_currentSortCol).Add(_sortAdorner);
            listViewReplays.Items.SortDescriptions.Add(new SortDescription(sortBy, newDir));

            // (CollectionViewSource.GetDefaultView(listViewReplays) as ListCollectionView).CustomSort = new PlayerSorter();
        }
        #endregion

        #region context menus

        #region parse tab

        private void RemoveFoundReplay_Click(object sender, RoutedEventArgs e)
        {
            MenuItem removeFoundReplay = sender as MenuItem;

            var replayFiles = replayFilesFoundListBox.SelectedItems.Cast<ParseFile>().ToList();
            foreach (var aRep in replayFiles)
            {
                _files.Remove(aRep);
            }
            replayFilesFoundListBox.Items.Refresh();
        }

        #endregion

        #region search tab

        private void OpenInFileExplorerMenuItem_Click(object sender, RoutedEventArgs e)
        {
            MenuItem openFileInExplorer = sender as MenuItem;
            ListViewItem listViewItemReplay = listViewReplays.ItemContainerGenerator.ContainerFromItem(openFileInExplorer.DataContext) as ListViewItem;
            var replay = listViewItemReplay.Content as File<IReplay>;
            OpenInFileExplorer(replay.FilePath);
        }

        //TODO shouldn't make a difference whether 1 or multiple are selected, rewrite...
        private void CopyFilePathMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (listViewReplays.SelectedItems.Count == 1)
            {
                MenuItem copyFilePath = sender as MenuItem;
                ListViewItem listViewItemReplay = listViewReplays.ItemContainerGenerator.ContainerFromItem(copyFilePath.DataContext) as ListViewItem;
                var replay = listViewItemReplay.Content as File<IReplay>;
                Clipboard.SetText(replay.FilePath);
            }
            else
            {
                var listViewItemReplays = listViewReplays.SelectedItems as System.Collections.IList;
                var copiedFilePaths = new StringBuilder();
                foreach (File<IReplay> aReplay in listViewItemReplays)
                {
                    copiedFilePaths.AppendLine(aReplay.FilePath);
                }
                Clipboard.SetText(copiedFilePaths.ToString());
            }
        }

        private void LaunchReplayInStarcraftMenuItem_Click(object sender, RoutedEventArgs e)
        {

        }

        //TODO make this undoable??
        private void RenameReplayMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var listViewItemReplays = listViewReplays.SelectedItems as System.Collections.IList;
            // MenuItem renameReplay = sender as MenuItem;
            // ListViewItem listViewItemReplay = listViewReplays.ItemContainerGenerator.ContainerFromItem(renameReplay.DataContext) as ListViewItem;
            // var replay = listViewItemReplay.Content as File<IReplay>;
            // Please enter a renaming syntax:
            // Rename replay
            CustomReplayFormat customReplayFormat = null;
            var replayDialog = new TextInputDialog("Rename replay", "Please enter a renaming syntax:");
            if (replayDialog.ShowDialog() == true)
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(replayDialog.Answer))
                        throw new ArgumentException();

                    customReplayFormat = CustomReplayFormat.Create(replayDialog.Answer, listViewItemReplays.Count, true);
                }
                catch(ArgumentException)
                {
                    MessageBox.Show("Invalid custom replay format. Check help section for correct syntax", "Invalid syntax", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);
                    return;
                }
            }
            else
            {
                return;
            }
            var replaysSuccessfullyRenamed = new List<File<IReplay>>();
            StringBuilder exceptions = new StringBuilder();
            exceptions.AppendLine("Failed to rename the following replays:");
            int numberOfFailedReplays = 0;

            foreach (File<IReplay> replay in listViewItemReplays)
            {
                try
                {
                    replay.AddAfterCurrent(Path.GetDirectoryName(replay.FilePath) + @"\" + ReplayHandler.GenerateReplayName(replay, customReplayFormat) + ".rep");
                    ReplayHandler.MoveReplay(replay, true);
                    //TODO investigate whether there actually was a binding active between the displaymember and FilePath...
                    Binding b = new Binding("FilePath");
                    GridView gv = (GridView)listViewReplays.View;
                    gv.Columns[4].DisplayMemberBinding = b;
                    // listViewReplays.Items.Refresh();
                    replaysSuccessfullyRenamed.Add(replay);
                }
                catch (Exception)
                {
                    numberOfFailedReplays++;
                    exceptions.AppendLine($"{numberOfFailedReplays}. {replay.FilePath}");
                }
            }
            
            AddUndoable(replaysSuccessfullyRenamed);
            if (numberOfFailedReplays > 0)
            {
                MessageBox.Show(exceptions.ToString(), "Rename exceptions", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);
            }
        }

        private void DetailsReplayMenuItem_Click(object sender, RoutedEventArgs e)
        {

        }

        #endregion

        #region rename tab

        private void RenameOutputOpenInFileExplorerMenuItem_Click(object sender, RoutedEventArgs e)
        {
            MenuItem openFileInExplorer = sender as MenuItem;
            ListViewItem listViewItemReplay = renameTransformationResultListView.ItemContainerGenerator.ContainerFromItem(openFileInExplorer.DataContext) as ListViewItem;
            var replay = listViewItemReplay.Content as ReplayRenamer.Renaming;
            OpenInFileExplorer(replay.Replay.FilePath);
        }

        //TODO how to make it scroll until item is the first in view?
        // I scroll to the last item and then back again lol...
        private void RenameOutputSelectInSearchView_Click(object sender, RoutedEventArgs e)
        {
            MenuItem openFileInExplorer = sender as MenuItem;
            ListViewItem listViewItemReplay = renameTransformationResultListView.ItemContainerGenerator.ContainerFromItem(openFileInExplorer.DataContext) as ListViewItem;
            var replay = listViewItemReplay.Content as ReplayRenamer.Renaming;
            searchTabItem.Focus();
            var lastItem = listViewReplays.Items.GetItemAt(listViewReplays.Items.Count - 1);
            listViewReplays.ScrollIntoView(lastItem);
            listViewReplays.SelectedItem = replay.Replay;
            listViewReplays.ScrollIntoView(replay.Replay);
        }

        #endregion

        #endregion

        #region help 

        private void HelpGuideMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var helpGuideWindow = new HelpWindow();
            helpGuideWindow.Show();
        }

        #endregion

    }
}

