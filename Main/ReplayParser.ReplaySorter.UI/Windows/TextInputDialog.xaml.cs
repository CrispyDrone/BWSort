using System;
using System.Windows;

namespace ReplayParser.ReplaySorter.UI.Windows
{
    /// <summary>
    /// Interaction logic for TextInputDialog.xaml
    /// </summary>
    public partial class TextInputDialog : Window
    {
        public TextInputDialog(string title, string question, string defaultAnswer = "")
        {
            InitializeComponent();
            Title = title;
            questionLable.Content = question;
            answerTextBox.Text = defaultAnswer;
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            answerTextBox.SelectAll();
            answerTextBox.Focus();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }

        public string Answer => answerTextBox.Text;
    }
}
