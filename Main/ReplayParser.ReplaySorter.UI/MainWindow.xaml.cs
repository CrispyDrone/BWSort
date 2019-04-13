using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using ReplayParser.Interfaces;
using ReplayParser.Loader;
using System.IO;
using System.Diagnostics;
using System.ComponentModel;
using Microsoft.WindowsAPICodePack.Dialogs;
using ReplayParser.ReplaySorter.Sorting;
using ReplayParser.ReplaySorter.ReplayRenamer;
using ReplayParser.ReplaySorter.Configuration;
using ReplayParser.ReplaySorter.Diagnostics;
using ReplayParser.ReplaySorter.IO;
using System.Net.Http;
using ReplayParser.ReplaySorter.Filtering;
using ReplayParser.ReplaySorter.UI.Windows;
using ReplayParser.ReplaySorter.UI.Sorting;
using System.Windows.Documents;
using System.Windows.Data;
using System.Text;

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
        private List<string> _files = new List<string>();
        private HashSet<string> _replayHashes = new HashSet<string>();
        private List<string> _replaysThrowingExceptions = new List<string>();
        private BackgroundWorker _worker_ReplayParser = null;
        private bool _moveBadReplays = false;
        private bool _movingBadReplays = false;
        // this feels silly, you can only use the state object passed to RunWorkAsync in ReportProgress
        private string _errorMessage = string.Empty;
        private string _badReplayDirectory = string.Empty;

        // sorting
        private Sorter _sorter;
        private BackgroundWorker _worker_ReplaySorter = null;
        private bool _sortingReplays = false;
        private bool _keepOriginalReplayNames = true;
        private BoolAnswer _boolAnswer = null;
        private Stopwatch _swSort = new Stopwatch();
        private List<string> SortCriteria = new List<string> { "none", "playername", "matchup", "map", "duration", "gametype" };
        private Dictionary<ComboBox, List<string>> ComboBoxSortCriteriaDictionary = new Dictionary<ComboBox, List<string>>();

        // renaming
        private BackgroundWorker _worker_ReplayRenamer = null;
        private bool _renamingReplays = false;

        // undoing
        private BackgroundWorker _worker_Undoer = null;
        private bool _undoingRename = false;

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
            InitializeSortCriteriaComboBoxes();
            EnableSortingAndRenamingButtons(ReplayAction.Parse, false);
            _replaySorterConfiguration = new ReplaySorterAppConfiguration();
            IntializeErrorLogger(_replaySorterConfiguration);
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
                        if (versionTag != _replaySorterConfiguration.Version)
                            MessageBox.Show($"A new version is available at {_replaySorterConfiguration.RepositoryUrl}");
                    }
                    catch (HttpRequestException ex)
                    {
                        statusBarErrors.Content = "Failed to check for updates.";
                        ErrorLogger.GetInstance()?.LogError("Failed to check for updates.", ex: ex);
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
                DiscoverReplayFiles();
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

        private void InitializeSortCriteriaComboBoxes()
        {
            foreach (var gridchild in gridContainingSortCriteriaComboBoxes.Children)
            {
                if (gridchild is StackPanel)
                {
                    foreach (var child in (gridchild as StackPanel).Children)
                    {
                        if (child is ComboBox)
                        {
                            var ComboBox = child as ComboBox;
                            var ItemsSource = new List<string>(SortCriteria);
                            ComboBox.ItemsSource = ItemsSource;
                            ComboBox.SelectedIndex = 0;
                            ComboBox.SelectionChanged += ComboBox_SelectionChanged;
                            ComboBoxSortCriteriaDictionary.Add(ComboBox, ItemsSource);
                        }
                    }
                }
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
                (sender as BackgroundWorker).ReportProgress(progressPercentage);
                ParseReplay(parsedReplays, hashedReplays, replay, checkForDuplicates);
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
                _movingBadReplays = true;
                currentPosition = 0;
                progressPercentage = 0;

                foreach (var replay in _replaysThrowingExceptions)
                {
                    currentPosition++;
                    progressPercentage = Convert.ToInt32(((double)currentPosition / _replaysThrowingExceptions.Count()) * 100);
                    (sender as BackgroundWorker).ReportProgress(progressPercentage);
                    ReplayHandler.RemoveBadReplay(_badReplayDirectory + @"\BadReplays", replay);
                }
            }
            _files = _files.Where(x => !_replaysThrowingExceptions.Contains(x)).ToList();
            e.Result = sw.Elapsed;
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

        private void ParseReplay(List<File<IReplay>> parsedReplays, HashSet<string> hashedReplays, string replay, bool checkForDuplicatesOnCumulativeParsing)
        {
            try
            {
                string hashedReplay = null;

                if (checkForDuplicatesOnCumulativeParsing)
                {
                    hashedReplay = HashReplay(replay);
                    if (_replayHashes.Contains(hashedReplay))
                        return;

                    hashedReplays.Add(hashedReplay);
                }

                ParseReplay(parsedReplays, replay, hashedReplay);
            }
            catch (Exception)
            {
                _replaysThrowingExceptions.Add(replay);
                _errorMessage = string.Format("Error with replay {0}", replay.ToString());
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

            if (e.UserState == null)
            {
                if (_errorMessage != string.Empty)
                {
                    statusBarErrors.Content = _errorMessage;
                }
                if (_movingBadReplays)
                {
                    statusBarAction.Content = "Moving bad replays...";
                }
            }
            else
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
                statusBarAction.Content = string.Format("Finished parsing!");
                MessageBox.Show(
                    string.Format("Parsing replays finished! It took {0} to parse {1} replays. {2} replays encountered exceptions during parsing. {3}",
                        (TimeSpan)e.Result,
                        _listReplays.Count(),
                        _replaysThrowingExceptions.Count(),
                        _moveBadReplays ? "Bad replays have been moved to the specified directory." : ""),
                    "Parsing summary",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information,
                    MessageBoxResult.OK);
                ResetReplayParsingVariables(false, true);
                EnableSortingAndRenamingButtons(ReplayAction.Parse, true);
            }
            listViewReplays.ItemsSource = _listReplays;
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
            _movingBadReplays = false;
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

        private void AddNewReplayFilesButton_Click(object sender, RoutedEventArgs e)
        {
            DiscoverReplayFiles();
        }

        private void DiscoverReplayFiles()
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

            var potentialfiles = Directory.EnumerateFiles(_replayDirectory, "*.rep", searchOption);

            if (potentialfiles.Count() == 0)
            {
                MessageBox.Show($"No replays found in {_replayDirectory}. Please specify an existing directory containing your replays.", "Failed to find replays.", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }

            _files.AddRange(potentialfiles);

            replayFilesFoundListBox.ItemsSource = _files;
            replayFilesFoundListBox.Items.Refresh();
        }

        private void ClearFoundReplayFilesButton_Click(object sender, RoutedEventArgs e)
        {
            ResetReplayParsingVariables(true, true);
            ResetFiltering();
            replayFilesFoundListBox.ItemsSource = null;
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
                    cancelUndoRenamingButton.IsEnabled = enable;
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
                    cancelUndoRenamingButton.IsEnabled = enable;
                    // renameInPlaceCheckBox.IsEnabled = enable;
                    // restoreOriginalReplayNamesCheckBox.IsEnabled = enable;
                    return;
                case ReplayAction.Rename:
                    parseReplaysButton.IsEnabled = enable;
                    cancelParsingButton.IsEnabled = enable;
                    executeSortButton.IsEnabled = enable;
                    cancelSortButton.IsEnabled = enable;
                    undoRenamingButton.IsEnabled = enable;
                    cancelUndoRenamingButton.IsEnabled = enable;
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

        #endregion

        #region sorting

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var ChangedComboBox = sender as ComboBox;
            var SelectedItem = (string)ChangedComboBox.SelectedItem;

            foreach (var combobox in ComboBoxSortCriteriaDictionary.Keys.ToList())
            {
                if (ChangedComboBox != combobox)
                {
                    var nonChangedComboBoxSelectedItem = (string)combobox.SelectedItem;
                    if (SelectedItem != "none")
                    {
                        ComboBoxSortCriteriaDictionary[combobox] = ComboBoxSortCriteriaDictionary[combobox].Where(x => x != SelectedItem).ToList();
                    }
                    if ((string)e.RemovedItems[0] != "none")
                    {
                        ComboBoxSortCriteriaDictionary[combobox].Add((string)e.RemovedItems[0]);
                    }
                    combobox.SelectionChanged -= ComboBox_SelectionChanged;
                    combobox.ItemsSource = null;
                    combobox.ItemsSource = ComboBoxSortCriteriaDictionary[combobox].OrderBy(x => KeepDefinedOrdering(x));
                    combobox.SelectedIndex = combobox.Items.IndexOf(nonChangedComboBoxSelectedItem);
                    combobox.SelectionChanged += ComboBox_SelectionChanged;
                }
            }
            UpdateSortCriteriaParametersControls(SelectedItem, (string)e.RemovedItems[0]);
        }

        private static int KeepDefinedOrdering(string item)
        {
            switch (item)
            {
                case "none":
                    return 1;
                case "playername":
                    return 2;
                case "matchup":
                    return 3;
                case "map":
                    return 4;
                case "duration":
                    return 5;
                case "gametype":
                    return 6;
                default:
                    return 7;
            }
        }

        private void UpdateSortCriteriaParametersControls(string selectedItem, string unselectedItem)
        {
            var PanelToAdd = GetSortCriteriaParametersPanel(selectedItem);
            if (PanelToAdd != null)
            {
                sortCriteriaParameters.Children.Add(PanelToAdd);
            }
            var PanelToRemove = GetPanelWithName(sortCriteriaParameters, unselectedItem.ToUpper());
            if (PanelToRemove != null)
            {
                sortCriteriaParameters.Children.Remove(PanelToRemove);
            }
        }

        private static Panel GetSortCriteriaParametersPanel(string selectedItem)
        {
            switch (selectedItem)
            {
                case "playername":
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
                    return playername;
                case "duration":
                    StackPanel duration = new StackPanel();
                    duration.Name = "DURATION";
                    duration.Orientation = Orientation.Horizontal;
                    Label DurationIntervalsLabel = new Label();
                    DurationIntervalsLabel.Content = "Duration intervals: ";
                    TextBox DurationIntervalsTextBox = new TextBox();
                    DurationIntervalsTextBox.MinWidth = 200;
                    duration.Children.Add(DurationIntervalsLabel);
                    duration.Children.Add(DurationIntervalsTextBox);
                    return duration;
                case "matchup":
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
                    return gametypesPanel;
                default:
                    return null;
            }
        }

        private static void All_Clicked(object sender, RoutedEventArgs e)
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

        private static Panel GetPanelWithName(Panel parent, string name)
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
            return null;
        }

        private void executeSortButton_Click(object sender, RoutedEventArgs e)
        {
            if (_worker_ReplaySorter != null && _worker_ReplaySorter.IsBusy)
                return;

            // get textboxes value, you could also take them from your dictionary keys, add a tag with a number, and sort on this number so it's not as hardcoded?
            string[] CriteriaStringOrder = new string[5];
            CriteriaStringOrder[0] = sortCriteriaOneComboBox.Text;
            CriteriaStringOrder[1] = sortCriteriaTwoComboBox.Text;
            CriteriaStringOrder[2] = sortCriteriaThreeComboBox.Text;
            CriteriaStringOrder[3] = sortCriteriaFourComboBox.Text;
            CriteriaStringOrder[4] = sortCriteriaFiveComboBox.Text;

            CriteriaStringOrder = CriteriaStringOrder.Where(x => x != "none").Select(x => x.ToUpper()).ToArray();

            if (CriteriaStringOrder.Length == 0)
            {
                MessageBox.Show("Please make a selection of sort criteria. Not all of them can be none!", "No sort criteria selected!", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);
                return;
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
                        customReplayFormat = new CustomReplayFormat(customFormatTextBox.Text);
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
            foreach (var chosencriteria in CriteriaStringOrder)
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
                _sorter = new Sorter(sortOutputDirectoryTextBox.Text, _listReplays);
                _sorter.CurrentDirectory = sortOutputDirectoryTextBox.Text;
            }
            else
            {
                MessageBox.Show(string.Format("Could not find directory {0}", sortOutputDirectoryTextBox.Text), "Failed to start sort: directory error", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);
                return;
            }

            SortCriteriaParameters = new SortCriteriaParameters(makefolderforwinner, makefolderforloser, validgametypes, durations);
            _sorter.SortCriteriaParameters = SortCriteriaParameters;

            var filterReplays = filterReplaysCheckBox.IsChecked.HasValue && filterReplaysCheckBox.IsChecked.Value;

            if (filterReplays)
            {
                if (_filteredListReplays == null)
                    MessageBox.Show("Can not execute sort since filter did not return any replays!", "Failed to start sort: invalid filter", MessageBoxButton.OK, MessageBoxImage.Warning, MessageBoxResult.OK);

                _sorter.ListReplays = _filteredListReplays;
            }
            else
            {
                _sorter.ListReplays = _listReplays;
            }

            if (CriteriaStringOrder.Length > 1)
            {
                _sorter.SortCriteria = (Criteria)Enum.Parse(typeof(Criteria), string.Join(",", CriteriaStringOrder));
            }
            else
            {
                _sorter.SortCriteria = (Criteria)Enum.Parse(typeof(Criteria), CriteriaStringOrder[0]);
            }
            _sorter.CriteriaStringOrder = CriteriaStringOrder;
            if (customReplayFormat != null)
                _sorter.CustomReplayFormat = customReplayFormat;

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
            _worker_ReplaySorter.RunWorkerAsync();
        }

        private void worker_SortReplays(object sender, DoWorkEventArgs e)
        {
            var SorterConditions = CheckSorterConditions(_sorter);

            if (SorterConditions.GoodToGo == true)
            {
                _sortingReplays = true;
                e.Result = _sorter.ExecuteSortAsync(_keepOriginalReplayNames, _worker_ReplaySorter, _replaysThrowingExceptions);
                ReplayHandler.LogBadReplays(_replaysThrowingExceptions, _replaySorterConfiguration.LogDirectory, $"{DateTime.Now} - Error while sorting replay: {{0}} with arguments {_sorter.ToString()}");

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

        private static BoolAnswer CheckSorterConditions(Sorter aSorter)
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
            disableSiblingCheckBoxAndRenamingStackPanel(sender as CheckBox, "restoreOriginalReplayNamesCheckBox");
        }

        private void disableSiblingCheckBoxAndRenamingStackPanel(CheckBox checkBox, string siblingName)
        {
            if (checkBox == null)
                return;

            if (!checkBox.IsChecked.HasValue)
                return;

            var siblingCheckBox = GetSiblingRenameCheckBox(checkBox, siblingName);
            if (siblingCheckBox == null)
                return;

            var replayRenamingOutputDirectoryStackPanel = GetRenamingStackPanel(checkBox);
            if (replayRenamingOutputDirectoryStackPanel == null)
                return;

            siblingCheckBox.IsEnabled = !checkBox.IsChecked.Value;
            replayRenamingOutputDirectoryStackPanel.IsEnabled = !checkBox.IsChecked.Value;
        }

        private CheckBox GetSiblingRenameCheckBox(CheckBox renameCheckBox, string name)
        {
            var stackPanel = renameCheckBox.Parent as StackPanel;
            if (stackPanel == null)
                return null;

            return stackPanel.Children.OfType<CheckBox>().SingleOrDefault(c => c.Name == name);
        }

        private StackPanel GetRenamingStackPanel(CheckBox renameCheckBox)
        {
            var stackPanel = renameCheckBox.Parent as StackPanel;
            if (stackPanel == null)
                return null;

            var dockPanel = (stackPanel.Parent as DockPanel);
            if (dockPanel == null)
                return null;

            return dockPanel.Children.OfType<StackPanel>().SingleOrDefault(s => s.Name == "replayRenamingOutputDirectoryStackPanel");
        }

        private void replayRenamingOutputDirectoryButton_Click(object sender, RoutedEventArgs e)
        {
            SelectMapFolder(replayRenamingOutputDirectoryTextBox);
        }

        private void executeRenamingButton_Click(object sender, RoutedEventArgs e)
        {
            if (_worker_ReplayRenamer != null && _worker_ReplayRenamer.IsBusy)
                return;

            bool? renameInPlace = renameInPlaceCheckBox.IsChecked;
            bool? restoreOriginalReplayNames = restoreOriginalReplayNamesCheckBox.IsChecked;
            string replayRenamingSyntax = replayRenamingSyntaxTextBox.Text;
            string replayRenamingOutputDirectory = replayRenamingOutputDirectoryTextBox.Text;

            var renameInPlaceValue = renameInPlace.HasValue && renameInPlace.Value;
            var restoreOriginalReplayNamesValue = restoreOriginalReplayNames.HasValue && restoreOriginalReplayNames.Value;
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
                    customReplayFormat = new CustomReplayFormat(replayRenamingSyntax);
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

            var renamingParameters = RenamingParameters.Create(customReplayFormat, replayRenamingOutputDirectory, renameInPlaceValue, restoreOriginalReplayNamesValue);

            if (renamingParameters == null)
            {
                MessageBox.Show("Please fill in a proper renaming format and an output directory, or tick off one of the checkboxes.", "Invalid parameters", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);
                return;
            }

            var filterReplays = filterReplaysCheckBox.IsChecked.HasValue && filterReplaysCheckBox.IsChecked.Value;
            Renamer replayRenamer = null;

            if (filterReplays)
            {
                if (_filteredListReplays == null)
                    MessageBox.Show("Can not execute rename since filter did not return any replays!", "Failed to start rename: invalid filter", MessageBoxButton.OK, MessageBoxImage.Warning, MessageBoxResult.OK);

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
            _worker_ReplayRenamer.RunWorkerAsync(replayRenamer);
        }

        private void worker_RenameReplays(object sender, DoWorkEventArgs e)
        {
            var replayRenamer = e.Argument as Renamer;
            var renameInPlace = replayRenamer.RenameInPlace;
            var restoreOriginalReplayNames = replayRenamer.RestoreOriginalReplayNames;
            ServiceResult<ServiceResultSummary<IEnumerable<File<IReplay>>>> response = null;
            _renamingReplays = true;

            if (renameInPlace)
            {
                response = replayRenamer.RenameInPlaceAsync(sender as BackgroundWorker);
            }
            else if (restoreOriginalReplayNames)
            {
                response = replayRenamer.RestoreOriginalNames(sender as BackgroundWorker);
            }
            else
            {
                // renaming into another directory
                response = replayRenamer.RenameToDirectoryAsync(sender as BackgroundWorker);
            }

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
            _renamingReplays = false;
            var response = e.Result as ServiceResult<ServiceResultSummary<List<File<IReplay>>>>;
            progressBarSortingReplays.Value = 0;

            AddUndoable(response.Result.Result);

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

        private void cancelRenamingButton_Click(object sender, RoutedEventArgs e)
        {
            if (_worker_ReplayRenamer != null && _worker_ReplayRenamer.IsBusy)
            {
                _worker_ReplayRenamer.CancelAsync();
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
            var response = e.Result as ServiceResult<ServiceResultSummary<IEnumerable<File<IReplay>>>>;
            progressBarRenamingOrRestoringReplays.Value = 0;

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

        private void cancelUndoRenamingButton_Click(object sender, RoutedEventArgs e)
        {
            if (_worker_Undoer != null && _worker_Undoer.IsBusy)
            {
                _worker_Undoer.CancelAsync();
            }
        }

        private void RestoreOriginalReplayNamesCheckBox_Click(object sender, RoutedEventArgs e)
        {
            disableSiblingCheckBoxAndRenamingStackPanel(sender as CheckBox, "renameInPlaceCheckBox");
            replayRenamingSyntaxTextBox.IsEnabled = !replayRenamingSyntaxTextBox.IsEnabled;
        }

        #endregion

        #region filtering


        private BackgroundWorker worker_replayFilterer;
        private ReplayFilterer _replayFilterer = new ReplayFilterer();
        private string _lastExecutedFilter;

        private void FilterReplaysTextBox_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            // TODO show message is busy ?
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                if (string.IsNullOrWhiteSpace(filterReplaysTextBox.Text))
                {
                    if (listViewReplays.ItemsSource != _listReplays)
                    {
                        listViewReplays.ItemsSource = _listReplays;
                        filterReplaysProgressBar.Value = 0;
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

        #region context menu

        private void OpenInFileExplorerMenuItem_Click(object sender, RoutedEventArgs e)
        {
            MenuItem openFileInExplorer = sender as MenuItem;
            ListViewItem listViewItemReplay = listViewReplays.ItemContainerGenerator.ContainerFromItem(openFileInExplorer.DataContext) as ListViewItem;
            var replay = listViewItemReplay.Content as File<IReplay>;
            var folderPath = Path.GetDirectoryName(replay.FilePath);
            if (!File.Exists(replay.FilePath))
            {
                return;
            }

            string argument = "/select, \"" + replay.FilePath + "\"";
            try
            {
                Process.Start("explorer.exe", argument);
            }
            catch (Exception)
            {
                MessageBox.Show("An error occurred while trying to open the file.", "Failed to open file.", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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

                    customReplayFormat = new CustomReplayFormat(replayDialog.Answer);
                }
                catch(ArgumentException)
                {
                    MessageBox.Show("Invalid custom replay format. Check help section for correct syntax", "Invalid syntax", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);
                    return;
                }
            }
            var replaysSuccessfullyRenamed = new List<File<IReplay>>();
            StringBuilder exceptions = new StringBuilder();
            exceptions.AppendLine("Failed to rename the following replays:");
            int numberOfFailedReplays = 0;

            foreach (File<IReplay> replay in listViewItemReplays)
            {
                try
                {
                    replay.AddAfterCurrent(Path.GetDirectoryName(replay.FilePath) + @"\" + ReplayHandler.GenerateReplayName(replay.Content, customReplayFormat) + ".rep");
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
    }
}

