using ReplayParser.Interfaces;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using System.Windows;
using System;

namespace ReplayParser.ReplaySorter.UI.Converters
{
    public class ObserverToVisibilityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var player = values[0] as IPlayer;
            if (player == null)
                return Visibility.Hidden;

            var file = values[1] as IO.File<IReplay>;
            if (file == null)
                return Visibility.Hidden;

            if (file.Content.Observers.Contains(player))
                return Visibility.Visible;

            return Visibility.Hidden;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
