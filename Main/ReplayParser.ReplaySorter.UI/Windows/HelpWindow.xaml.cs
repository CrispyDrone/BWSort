using ReplayParser.ReplaySorter.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System;

namespace ReplayParser.ReplaySorter.UI.Windows
{
    /// <summary>
    /// Interaction logic for HelpWindow.xaml
    /// </summary>
    public partial class HelpWindow : Window
    {
        public HelpWindow()
        {
            InitializeComponent();
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                var readmeUri = new Uri("README.md", UriKind.Relative);
                var stream = Application.GetContentStream(readmeUri);
                using (var fs = new StreamReader(stream.Stream, Encoding.UTF8))
                {
                    userGuideMarkdownViewer.Markdown = await fs.ReadToEndAsync();
                }
            }
            catch (Exception ex)
            {
                ErrorLogger.GetInstance()?.LogError($"{DateTime.Now} - Failed to open stream for README.md.", ex: ex);
                MessageBox.Show("Failed to load README.md, please make sure you did not delete this file located in the root directory.", "Error loading README.md file", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK); ;
                Close();
            }
        }
    }
}
