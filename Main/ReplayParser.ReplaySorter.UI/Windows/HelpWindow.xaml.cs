using ReplayParser.ReplaySorter.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System;
using Markdig;
using Markdig.Wpf;
using System.Diagnostics;
using System.Windows.Documents;
using System.Text.RegularExpressions;
using System.Collections.Generic;

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
                    userGuideMarkdownViewer.Pipeline = new MarkdownPipelineBuilder()
                        .UseSupportedExtensions()
                        .Build();
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

        private static readonly object _lock = new object();
        private static Dictionary<string, Paragraph> _paragraphDictionary = new Dictionary<string, Paragraph>();
        private static string _relativeLink = @"\[{0}]\((#.*?)\)";
        private void OpenHyperlink(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            if (e.Parameter.ToString().Contains("http"))
                Process.Start(e.Parameter.ToString());
            else
            {
                var hyperLink = e.OriginalSource as Hyperlink;
                var text = new TextRange(hyperLink.ContentStart, hyperLink.ContentEnd).Text;
                var markdown = userGuideMarkdownViewer.Markdown;
                var index = markdown.IndexOf(text);
                var relativeLinkMatch = new Regex(string.Format(_relativeLink, text)).Match(markdown.Substring(index - 1));
                if (!relativeLinkMatch.Success)
                    return;

                var relativeLink = relativeLinkMatch.Groups[1].Value;
                relativeLink = relativeLink.TrimStart('#').Replace('-', ' ');
                lock (_lock)
                {
                    if (_paragraphDictionary.ContainsKey(relativeLink))
                    {
                        _paragraphDictionary[relativeLink].BringIntoView();
                        return;
                    }
                }

                var enumerator = userGuideMarkdownViewer.Document.Blocks.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    var paragraph = enumerator.Current as Paragraph;
                    if (paragraph == null)
                        continue;

                    foreach (var inline in paragraph.Inlines)
                    {
                        var run = inline as Run;
                        if (run == null)
                            continue;

                        if (run.Text.ToLower() == relativeLink.ToLower())
                        {
                            lock (_lock)
                            {
                                if (!_paragraphDictionary.ContainsKey(relativeLink))
                                {
                                    _paragraphDictionary.Add(relativeLink.ToLower(), paragraph);
                                }
                            }
                            paragraph.BringIntoView();
                            return;
                        }
                    }
                }
            }
        }
    }
}
