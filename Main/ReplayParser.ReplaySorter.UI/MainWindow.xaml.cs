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
        }

        // parsing
        private List<File<IReplay>> ListReplays = new List<File<IReplay>>();
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

        private void parseReplaysButton_Click(object sender, RoutedEventArgs e)
        {
            SortingButtons(false);

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
            SearchDirectory searchDirectory = new SearchDirectory(replayDirectoryTextBox.Text, searchOption);
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

            worker_ReplayParser = new BackgroundWorker();
            worker_ReplayParser.WorkerReportsProgress = true;
            worker_ReplayParser.WorkerSupportsCancellation = true;
            worker_ReplayParser.DoWork += worker_ParseReplays;
            worker_ReplayParser.ProgressChanged += worker_ProgressChangedParsingReplays;
            worker_ReplayParser.RunWorkerCompleted += worker_ParsingReplaysCompleted;
            // sigh... should I make some sort of new class that contains all the properties I want to access during the DoWork ??
            worker_ReplayParser.RunWorkerAsync(searchDirectory);
        }

        private void cancelParsingButton_Click(object sender, RoutedEventArgs e)
        {
            if (worker_ReplayParser != null)
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
                    ListReplays.Add(new File<IReplay> { Content = ParsedReplay, FileName = replay });
                }
                catch (Exception)
                {
                    ReplaysThrowingExceptions.Add(replay);
                    ErrorMessage = string.Format("Error with replay {0}", replay.ToString());
                }
            }
            sw.Stop();
            ErrorMessage = string.Empty;
            ReplayHandler.LogBadReplays(ReplaysThrowingExceptions, ((SearchDirectory)e.Argument).Directory);
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
                    MessageBox.Show(string.Format("Parsing replays finished! It took {0} seconds to parse {1} replays. {2} replays encountered exceptions during parsing. {3}", (TimeSpan)e.Result, ListReplays.Count(), ReplaysThrowingExceptions.Count(), MoveBadReplays ? "Bad replays have been moved to the specified directory." : ""), "Parsing summary", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK);
                    ResetReplayParsingVariables(false, true);
                }
            }
            SortingButtons(true);
        }

        private void ResetReplayParsingVariables(bool clearListReplays, bool resetMoveBadReplays)
        {
            if (clearListReplays)
            {
                ListReplays.Clear();
            }
            if (resetMoveBadReplays)
            {
                MoveBadReplays = false;
            }
            ReplaysThrowingExceptions.Clear();
            NoReplaysFound = false;
            MovingBadReplays = false;
        }


        private void SortingButtons(bool enable)
        {
            if (enable)
            {
                executeSortButton.IsEnabled = true;
                cancelSortButton.IsEnabled = true;
                previewSortButton.IsEnabled = true;
                cancelPreviewSortButton.IsEnabled = true;
            }
            else
            {
                executeSortButton.IsEnabled = false;
                cancelSortButton.IsEnabled = false;
                previewSortButton.IsEnabled = false;
                cancelPreviewSortButton.IsEnabled = false;
            }
        }

        private void replayDirectoryButton_Click(object sender, RoutedEventArgs e)
        {
            //var folderDialog = new CommonOpenFileDialog();
            //folderDialog.IsFolderPicker = true;
            //if (folderDialog.ShowDialog() == CommonFileDialogResult.Ok)
            //{
            //    replayDirectoryTextBox.Text = folderDialog.FileName;
            //}
            SelectMapFolder(replayDirectoryTextBox);
        }

        private void badReplayDirectory_Click(object sender, RoutedEventArgs e)
        {
            //var folderDialog = new CommonOpenFileDialog();
            //folderDialog.IsFolderPicker = true;
            //if (folderDialog.ShowDialog() == CommonFileDialogResult.Ok)
            //{
            //    moveBadReplaysDirectory.Text = folderDialog.FileName;
            //}
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
                case "gametype":
                    StackPanel gametypesPanel = new StackPanel();
                    gametypesPanel.Name = "MATCHUP";
                    gametypesPanel.Orientation = Orientation.Vertical;
                    CheckBox All = new CheckBox();
                    All.Content = "All";
                    All.Name = "All";
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
                else if (chosencriteria == "GAMETYPE")
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
                MessageBox.Show(String.Format("Could not find directory {0}", sortOutputDirectoryTextBox.Text), "Failed to start sort: directory error", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);
                return;
            }

            SortCriteriaParameters = new SortCriteriaParameters(makefolderforwinner, makefolderforloser, validgametypes, durations);
            sorter.SortCriteriaParameters = SortCriteriaParameters;

            sorter.ListReplays = ListReplays;
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

            worker_ReplaySorter = new BackgroundWorker();
            worker_ReplaySorter.WorkerReportsProgress = true;
            worker_ReplaySorter.WorkerSupportsCancellation = true;
            worker_ReplaySorter.DoWork += worker_SortReplays;
            worker_ReplaySorter.ProgressChanged += worker_ProgressChangedSortingReplays;
            worker_ReplaySorter.RunWorkerCompleted += worker_SortingReplaysCompleted;
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
                MessageBox.Show(string.Format("Finished sorting replays! It took {0} seconds to sort {1} replays. {2} replays encountered exceptions.", swSort.Elapsed, ListReplays.Count, ReplaysThrowingExceptions.Count()), "Finished Sorting", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK);
                ReplayHandler.LogBadReplays(ReplaysThrowingExceptions, sorter.OriginalDirectory + @"\failedSorts");
                ResetReplaySortingVariables();
                ReplaysThrowingExceptions.Clear();
            }
        }

        private void ResetReplaySortingVariables()
        {
            // not implemented
            SortingReplays = false;
            KeepOriginalReplayNames = true;
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
            if (worker_ReplaySorter != null)
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
        }

        private void MenuItemClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}

