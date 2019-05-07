using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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
            // switch (backupAction)
            // {
            //     case BackupAction.Create:
            //         actionContentUri = new Uri("/BackupActions/CreateBackupActionContent.xaml", UriKind.Relative);
            //         break;
            //     case BackupAction.Delete:
            //         actionContentUri = new Uri("/BackupActions/DeleteBackupActionContent.xaml", UriKind.Relative);
            //         break;
            //     case BackupAction.Inspect:
            //         actionContentUri = new Uri("/BackupActions/InspectBackupActionContent.xaml", UriKind.Relative);
            //         break;
            //     case BackupAction.Restore:
            //         actionContentUri = new Uri("/BackupActions/RestoreActionContent.xaml", UriKind.Relative);
            //         break;
            //     default:
            //         throw new ArgumentException(nameof(backupAction));
            // }

            var resourceStreamInfo = Application.GetResourceStream(actionContentUri);
            var xamlReader = new XamlReader();
            //TODO investigate LoadAsync but no awaiter implemented...
            return xamlReader.LoadAsync(resourceStreamInfo.Stream) as UIElement;
        }

        #endregion

        #endregion

        #region public

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
