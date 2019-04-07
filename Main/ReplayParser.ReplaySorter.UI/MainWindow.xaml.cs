using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using ReplayParser.Interfaces;
using ReplayParser.Loader;
using ReplayParser.ReplaySorter.UserInput;
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
using System.Windows.Data;
using ReplayParser.ReplaySorter.Filtering;
using System.Configuration;
using ReplayParser.ReplaySorter.UI.Windows;

namespace ReplayParser.ReplaySorter.UI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
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
            if (_replaySorterConfiguration.LoadReplaysOnStartup)
            {
                parseReplays();
            }
        }

        // configuration
        private IReplaySorterConfiguration _replaySorterConfiguration;

        // parsing
        private string replayDirectory;
        private List<File<IReplay>> ListReplays;
        private IEnumerable<string> files;
        List<string> ReplaysThrowingExceptions = new List<string>();
        private BackgroundWorker worker_ReplayParser = null;
        private bool NoReplaysFound = false;
        private bool MoveBadReplays = false;
        private bool MovingBadReplays = false;
        // this feels silly, you can only use the state object passed to RunWorkAsync in ReportProgress
        private string ErrorMessage = string.Empty;
        private string BadReplayDirectory = string.Empty;

        // sorting
        private Sorter sorter;
        private BackgroundWorker worker_ReplaySorter = null;
        bool SortingReplays = false;
        bool KeepOriginalReplayNames = true;
        BoolAnswer boolAnswer = null;
        Stopwatch swSort = new Stopwatch();

        // renaming
        private BackgroundWorker worker_ReplayRenamer = null;
        private bool RenamingReplays = false;

        // undoing
        private BackgroundWorker worker_Undoer = null;
        private bool UndoingRename = false;

        // renaming and undoing
        //TODO
        LinkedList<List<File<IReplay>>> _renamedReplaysList = new LinkedList<List<File<IReplay>>>();
        LinkedListNode<List<File<IReplay>>> _renamedReplayListHead;

        // filtering
        //TOOD
        private List<File<IReplay>> _filteredListReplays;

        private void IntializeErrorLogger(IReplaySorterConfiguration replaySorterConfiguration)
        {
            if (ErrorLogger.GetInstance(replaySorterConfiguration) == null)
            {
                MessageBox.Show("Issue intializing logger. Logging will be disabled.", "Logger failure", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);
            }
        }

        private void parseReplaysButton_Click(object sender, RoutedEventArgs e)
        {
            parseReplays();
        }

        private void parseReplays()
        {
            if (worker_ReplayParser != null && worker_ReplayParser.IsBusy)
                return;

            SearchOption searchOption;
            if (includeSubdirectoriesCheckbox.IsChecked == true)
                searchOption = SearchOption.AllDirectories;
            else
                searchOption = SearchOption.TopDirectoryOnly;
            if (!Directory.Exists(replayDirectoryTextBox.Text))
            {
                MessageBox.Show("The specified directory does not exist.", "Invalid directory", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);
                return;
            }

            replayDirectory = replayDirectoryTextBox.Text;
            SearchDirectory searchDirectory = new SearchDirectory(replayDirectory, searchOption);

            statusBarAction.Content = "Parsing replays...";
            if (moveBadReplaysCheckBox.IsChecked == true)
            {
                MoveBadReplays = true;
                BadReplayDirectory = moveBadReplaysDirectory.Text;
                if (!Directory.Exists(BadReplayDirectory))
                {
                    MessageBox.Show("The specified bad replay directory does not exist.", "Invalid directory", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);
                    return;
                }
            }

            if (worker_ReplayParser == null)
            {
                worker_ReplayParser = new BackgroundWorker();
                worker_ReplayParser.WorkerReportsProgress = true;
                worker_ReplayParser.WorkerSupportsCancellation = true;
                worker_ReplayParser.DoWork += worker_ParseReplays;
                worker_ReplayParser.ProgressChanged += worker_ProgressChangedParsingReplays;
                worker_ReplayParser.RunWorkerCompleted += worker_ParsingReplaysCompleted;
            }
            // sigh... should I make some sort of new class that contains all the properties I want to access during the DoWork ??
            worker_ReplayParser.RunWorkerAsync(searchDirectory);

        }

        private void cancelParsingButton_Click(object sender, RoutedEventArgs e)
        {
            if (worker_ReplayParser != null && worker_ReplayParser.IsBusy)
            {
                worker_ReplayParser.CancelAsync();
            }
        }

        private void worker_ParseReplays(object sender, DoWorkEventArgs e)
        {
            var Potentialfiles = Directory.EnumerateFiles(((SearchDirectory)e.Argument).Directory, "*.rep", ((SearchDirectory)e.Argument).SearchOption);

            if (Potentialfiles.Count() == 0)
            {
                // how can I do this differently? Or is this the way to go about it? Define a bool to remember if the cancel is due to no replays, or due to a user...
                // ...  how to break properly?
                NoReplaysFound = true;
                e.Result = e.Argument;
                return;
            }
            files = Potentialfiles;
            ResetReplayParsingVariables(true, false);
            Stopwatch sw = new Stopwatch();
            sw.Start();
            int currentPosition = 0;
            int progressPercentage = 0;
            var parsedReplays = new List<File<IReplay>>();

            foreach (var replay in files)
            {
                if (worker_ReplayParser.CancellationPending == true)
                {
                    e.Cancel = true;
                    return;
                }
                currentPosition++;
                progressPercentage = Convert.ToInt32(((double)currentPosition / files.Count()) * 100);
                (sender as BackgroundWorker).ReportProgress(progressPercentage);
                try
                {
                    var ParsedReplay = ReplayLoader.LoadReplay(replay);
                    parsedReplays.Add(File<IReplay>.Create(ParsedReplay, replay));
                }
                catch (Exception)
                {
                    ReplaysThrowingExceptions.Add(replay);
                    ErrorMessage = string.Format("Error with replay {0}", replay.ToString());
                }
            }
            ListReplays = parsedReplays;
            sw.Stop();
            ErrorMessage = string.Empty;
            ReplayHandler.LogBadReplays(ReplaysThrowingExceptions, _replaySorterConfiguration.LogDirectory, $"{DateTime.Now} - Error while parsing replay: {{0}}");
            if (MoveBadReplays == true)
            {
                MovingBadReplays = true;
                currentPosition = 0;
                progressPercentage = 0;

                foreach (var replay in ReplaysThrowingExceptions)
                {
                    currentPosition++;
                    progressPercentage = Convert.ToInt32(((double)currentPosition / ReplaysThrowingExceptions.Count()) * 100);
                    (sender as BackgroundWorker).ReportProgress(progressPercentage);
                    ReplayHandler.RemoveBadReplay(BadReplayDirectory + @"\BadReplays", replay);
                }
            }
            files = files.Where(x => !ReplaysThrowingExceptions.Contains(x));
            e.Result = sw.Elapsed;
        }

        private void worker_ProgressChangedParsingReplays(object sender, ProgressChangedEventArgs e)
        {
            if (e.UserState == null)
            {
                progressBarParsingReplays.Value = e.ProgressPercentage;
                if (ErrorMessage != string.Empty)
                {
                    statusBarErrors.Content = ErrorMessage;
                }
                if (MovingBadReplays)
                {
                    statusBarAction.Content = "Moving bad replays...";
                }
            }
        }

        private void worker_ParsingReplaysCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Cancelled == true)
            {
                statusBarAction.Content = "Parsing cancelled...";
                ResetReplayParsingVariables(true, true);
                progressBarParsingReplays.Value = 0;
            }
            else
            {
                if (NoReplaysFound)
                    MessageBox.Show($"No replays found in {((SearchDirectory)e.Result).Directory}. Please specify an existing directory containing your replays.", "Failed to find replays.", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                else
                {
                    statusBarAction.Content = string.Format("Finished parsing!");
                    MessageBox.Show(
                        string.Format("Parsing replays finished! It took {0} to parse {1} replays. {2} replays encountered exceptions during parsing. {3}", 
                            (TimeSpan)e.Result,
                            ListReplays.Count(), 
                            ReplaysThrowingExceptions.Count(), 
                            MoveBadReplays ? "Bad replays have been moved to the specified directory." : ""), 
                        "Parsing summary", 
                        MessageBoxButton.OK, 
                        MessageBoxImage.Information, 
                        MessageBoxResult.OK);
                    ResetReplayParsingVariables(false, true);
                    EnableSortingAndRenamingButtons(ReplayAction.Parse, true);
                }
            }
            listViewReplays.ItemsSource = ListReplays;
        }

        private void ResetReplayParsingVariables(bool clearListReplays, bool resetMoveBadReplays)
        {
            if (clearListReplays)
            {
                ListReplays?.Clear();
            }
            if (resetMoveBadReplays)
            {
                MoveBadReplays = false;
            }
            ReplaysThrowingExceptions?.Clear();
            NoReplaysFound = false;
            MovingBadReplays = false;
        }


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

        private void SelectMapFolder(TextBox textbox)
        {
            var folderDialog = new CommonOpenFileDialog();
            folderDialog.IsFolderPicker = true;
            if (folderDialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                textbox.Text = folderDialog.FileName;
            }
        }

        private List<string> SortCriteria = new List<string> { "none", "playername", "matchup", "map", "duration", "gametype" };
        private Dictionary<ComboBox, List<string>> ComboBoxSortCriteriaDictionary = new Dictionary<ComboBox, List<string>>();
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
            if (worker_ReplaySorter != null && worker_ReplaySorter.IsBusy)
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
            KeepOriginalReplayNames = (bool)keepOriginalReplayNamesCheckBox.IsChecked;
            CustomReplayFormat customReplayFormat = null;

            if (!KeepOriginalReplayNames)
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
                sorter = new Sorter(sortOutputDirectoryTextBox.Text, ListReplays);
                sorter.CurrentDirectory = sortOutputDirectoryTextBox.Text;
            }
            else
            {
                MessageBox.Show(string.Format("Could not find directory {0}", sortOutputDirectoryTextBox.Text), "Failed to start sort: directory error", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);
                return;
            }

            SortCriteriaParameters = new SortCriteriaParameters(makefolderforwinner, makefolderforloser, validgametypes, durations);
            sorter.SortCriteriaParameters = SortCriteriaParameters;

            var filterReplays = filterReplaysCheckBox.IsChecked.HasValue && filterReplaysCheckBox.IsChecked.Value;

            if (filterReplays)
            {
                if (_filteredListReplays == null)
                    MessageBox.Show("Can not execute sort since filter did not return any replays!", "Failed to start sort: invalid filter", MessageBoxButton.OK, MessageBoxImage.Warning, MessageBoxResult.OK);

                sorter.ListReplays = _filteredListReplays;
            }
            else
            {
                sorter.ListReplays = ListReplays;
            }

            if (CriteriaStringOrder.Length > 1)
            {
                sorter.SortCriteria = (Criteria)Enum.Parse(typeof(Criteria), string.Join(",", CriteriaStringOrder));
            }
            else
            {
                sorter.SortCriteria = (Criteria)Enum.Parse(typeof(Criteria), CriteriaStringOrder[0]);
            }
            sorter.CriteriaStringOrder = CriteriaStringOrder;
            if (customReplayFormat != null)
                sorter.CustomReplayFormat = customReplayFormat;

            if (worker_ReplaySorter == null)
            {
                worker_ReplaySorter = new BackgroundWorker();
                worker_ReplaySorter.WorkerReportsProgress = true;
                worker_ReplaySorter.WorkerSupportsCancellation = true;
                worker_ReplaySorter.DoWork += worker_SortReplays;
                worker_ReplaySorter.ProgressChanged += worker_ProgressChangedSortingReplays;
                worker_ReplaySorter.RunWorkerCompleted += worker_SortingReplaysCompleted;
            }
            swSort.Start();
            worker_ReplaySorter.RunWorkerAsync();
        }

        private void worker_SortReplays(object sender, DoWorkEventArgs e)
        {
            var SorterConditions = CheckSorterConditions(sorter);

            if (SorterConditions.GoodToGo == true)
            {
                SortingReplays = true;
                e.Result = sorter.ExecuteSortAsync(KeepOriginalReplayNames, worker_ReplaySorter, ReplaysThrowingExceptions);
                ReplayHandler.LogBadReplays(ReplaysThrowingExceptions, _replaySorterConfiguration.LogDirectory, $"{DateTime.Now} - Error while sorting replay: {{0}} with arguments {sorter.ToString()}");

                if (worker_ReplaySorter.CancellationPending == true)
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
                boolAnswer = SorterConditions;
                //e.Result = SorterConditions;
                return;
            }
        }

        private void worker_ProgressChangedSortingReplays(object sender, ProgressChangedEventArgs e)
        {
            if (e.UserState == null)
            {
                progressBarSortingReplays.Value = e.ProgressPercentage;
                if (ErrorMessage != string.Empty)
                {
                    statusBarErrors.Content = ErrorMessage;
                }
                if (SortingReplays)
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
            swSort.Stop();
            if (e.Cancelled == true)
            {
                statusBarAction.Content = "Sorting cancelled...";
                progressBarSortingReplays.Value = 0;

                if (boolAnswer != null)
                {
                    MessageBox.Show(boolAnswer.Message, "Failed to start sort", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);
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
                MessageBox.Show(string.Format("Finished sorting replays! It took {0} to sort {1} replays. {2} replays encountered exceptions.", swSort.Elapsed, ListReplays.Count, ReplaysThrowingExceptions.Count()), "Finished Sorting", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK);
                ResetReplaySortingVariables();
                ReplaysThrowingExceptions.Clear();
            }
        }

        private void ResetReplaySortingVariables()
        {
            // not implemented
            SortingReplays = false;
            // KeepOriginalReplayNames = true;
            boolAnswer = null;
            swSort.Reset();
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
            if (worker_ReplaySorter != null && worker_ReplaySorter.IsBusy)
            {
                worker_ReplaySorter.CancelAsync();
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

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to close BWSort?", "Close BWSort", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) == MessageBoxResult.No)
            {
                e.Cancel = true;
            }
            if (_replaySorterConfiguration.RememberParsingDirectory)
            {
                _replaySorterConfiguration.LastParsingDirectory = replayDirectoryTextBox.Text;
            }
        }

        private void MenuItemClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

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
            if (worker_ReplayRenamer != null && worker_ReplayRenamer.IsBusy)
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
                if (ListReplays.All(r => r.FilePath == r.OriginalFilePath))
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
                replayRenamer = new Renamer(renamingParameters, ListReplays);
            }

            statusBarAction.Content = "Renaming replays...";

            if (worker_ReplayRenamer == null)
            {
                worker_ReplayRenamer = new BackgroundWorker();
                worker_ReplayRenamer.WorkerReportsProgress = true;
                worker_ReplayRenamer.WorkerSupportsCancellation = true;
                worker_ReplayRenamer.DoWork += worker_RenameReplays;
                worker_ReplayRenamer.ProgressChanged += worker_ProgressChangedRenamingReplays;
                worker_ReplayRenamer.RunWorkerCompleted += worker_RenamingReplaysCompleted;
            }
            worker_ReplayRenamer.RunWorkerAsync(replayRenamer);
        }

        private void worker_RenameReplays(object sender, DoWorkEventArgs e)
        {
            var replayRenamer = e.Argument as Renamer;
            var renameInPlace = replayRenamer.RenameInPlace;
            var restoreOriginalReplayNames = replayRenamer.RestoreOriginalReplayNames;
            ServiceResult<ServiceResultSummary<List<File<IReplay>>>> response = null;
            RenamingReplays = true;

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

            ReplayHandler.LogBadReplays(ReplaysThrowingExceptions, _replaySorterConfiguration.LogDirectory, $"{DateTime.Now} - Error while renaming replay: {{0}} using arguments: {replayRenamer.ToString()}");
            e.Result = response;
        }

        private void worker_ProgressChangedRenamingReplays(object sender, ProgressChangedEventArgs e)
        {
            if (e.UserState == null)
            {
                progressBarRenamingOrRestoringReplays.Value = e.ProgressPercentage;
                if (ErrorMessage != string.Empty)
                {
                    statusBarErrors.Content = ErrorMessage;
                }
                if (RenamingReplays)
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
            RenamingReplays = false;
            var response = e.Result as ServiceResult<ServiceResultSummary<List<File<IReplay>>>>;
            progressBarSortingReplays.Value = 0;

            if (_renamedReplayListHead == null)
            {
                _renamedReplaysList.AddFirst(response.Result.Result);
                _renamedReplayListHead = _renamedReplaysList.First;
            }
            else
            {
                while (_renamedReplayListHead.Next != null)
                {
                    _renamedReplaysList.Remove(_renamedReplayListHead.Next);
                }
                _renamedReplaysList.AddAfter(_renamedReplayListHead, response.Result.Result);
                _renamedReplayListHead = _renamedReplayListHead.Next;
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
        }

        private void cancelRenamingButton_Click(object sender, RoutedEventArgs e)
        {
            if (worker_ReplayRenamer != null && worker_ReplayRenamer.IsBusy)
            {
                worker_ReplayRenamer.CancelAsync();
            }
        }

        private void undoRenamingButton_Click(object sender, RoutedEventArgs e)
        {
            var isRenamed = _renamedReplayListHead != _renamedReplaysList?.First;

            if (!isRenamed)
            {
                MessageBox.Show("Please execute a rename in place before attempting to undo one.", "Invalid undo action", MessageBoxButton.OK, MessageBoxImage.Warning, MessageBoxResult.OK);
                return;
            }

            if (worker_Undoer == null)
            {
                worker_Undoer = new BackgroundWorker();
                worker_Undoer.WorkerReportsProgress = true;
                worker_Undoer.WorkerSupportsCancellation = true;
                worker_Undoer.DoWork += worker_UndoRename;
                worker_Undoer.ProgressChanged += worker_ProgressChangedUndoRename;
                worker_Undoer.RunWorkerCompleted += worker_UndoRenamingCompleted;
            }
            worker_Undoer.RunWorkerAsync(true);
        }

        private void redoRenamingButton_Click(object sender, RoutedEventArgs e)
        {
            var canRedo = _renamedReplayListHead != _renamedReplaysList.Last;

            if (!canRedo)
            {
                MessageBox.Show("Please execute an undo rename in place before attempting to redo one.", "Invalid redo action", MessageBoxButton.OK, MessageBoxImage.Warning, MessageBoxResult.OK);
                return;
            }

            if (worker_Undoer == null)
            {
                worker_Undoer = new BackgroundWorker();
                worker_Undoer.WorkerReportsProgress = true;
                worker_Undoer.WorkerSupportsCancellation = true;
                worker_Undoer.DoWork += worker_UndoRename;
                worker_Undoer.ProgressChanged += worker_ProgressChangedUndoRename;
                worker_Undoer.RunWorkerCompleted += worker_UndoRenamingCompleted;
            }
            worker_Undoer.RunWorkerAsync(false);
        }

        private void worker_UndoRename(object sender, DoWorkEventArgs e)
        {
            if ((bool)e.Argument == true)
            {
                // undo
                UndoingRename = true;

                _renamedReplayListHead = _renamedReplayListHead.Previous;

                var replayRenamer = new Renamer(RenamingParameters.Default, _renamedReplayListHead.Value);
                e.Result = replayRenamer.UndoRename(sender as BackgroundWorker);
            }
            else
            {
                // redo
                _renamedReplayListHead = _renamedReplayListHead.Next;

                var replayRenamer = new Renamer(RenamingParameters.Default, _renamedReplayListHead.Value);
                e.Result = replayRenamer.RedoRename(sender as BackgroundWorker);
            }
        }

        private void worker_ProgressChangedUndoRename(object sender, ProgressChangedEventArgs e)
        {
            if (e.UserState == null)
            {
                progressBarRenamingOrRestoringReplays.Value = e.ProgressPercentage;
                if (ErrorMessage != string.Empty)
                {
                    statusBarErrors.Content = ErrorMessage;
                }
                if (UndoingRename)
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
            var response = e.Result as ServiceResult<ServiceResultSummary>;
            progressBarRenamingOrRestoringReplays.Value = 0;

            if (e.Cancelled)
            {
                statusBarAction.Content = UndoingRename ? "Undoing renaming cancelled..." : "Redoing renaming cancelled...";
                MessageBox.Show(response.Result.Message, UndoingRename ? "Undoing renaming cancelled" : "Redoing renaming cancelled", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK);
            }
            else
            {
                statusBarAction.Content = UndoingRename ? "Finished undoing last renaming." : "Finished redoing last renaming";

                if (response.Success)
                {
                    MessageBox.Show(response.Result.Message, UndoingRename ? "Finished undoing last renaming" : "Finished redoing last renaming", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK);
                }
                else
                {
                    MessageBox.Show(string.Join(". ", response.Errors), UndoingRename ? "Failed undoing last renaming" : "Failed redoing last renaming", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);
                }
            }

            UndoingRename = false;
        }

        private void cancelUndoRenamingButton_Click(object sender, RoutedEventArgs e)
        {
            if (worker_Undoer != null && worker_Undoer.IsBusy)
            {
                worker_Undoer.CancelAsync();
            }
        }

        private void RestoreOriginalReplayNamesCheckBox_Click(object sender, RoutedEventArgs e)
        {
            disableSiblingCheckBoxAndRenamingStackPanel(sender as CheckBox, "renameInPlaceCheckBox");
            replayRenamingSyntaxTextBox.IsEnabled = !replayRenamingSyntaxTextBox.IsEnabled;
        }

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
                    if (listViewReplays.ItemsSource != ListReplays)
                        listViewReplays.ItemsSource = ListReplays;

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
                    // worker_replayFilterer.WorkerReportsProgress = true;
                    // worker_replayFilterer.WorkerSupportsCancellation = true;
                    worker_replayFilterer.DoWork += worker_FilterReplays;
                    // worker_replayFilterer.ProgressChanged += worker_ProgressChangedFilteringReplays;
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

            _filteredListReplays = _replayFilterer.Apply(ListReplays, filterExpression);
            e.Result = filterExpression;
        }

        //TODO small progress bar or waiting cursor?
        private void worker_ProgressChangedFilteringReplays(object sender, ProgressChangedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void worker_FilteringReplaysCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (!e.Cancelled && e.Error == null)
            {
                listViewReplays.ItemsSource = _filteredListReplays;
                _lastExecutedFilter = e.Result as string;
            }
        }

        #endregion

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
    }
}

