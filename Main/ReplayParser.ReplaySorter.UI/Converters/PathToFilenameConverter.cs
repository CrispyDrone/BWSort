using System;
using System.Globalization;
using System.IO;
using System.Windows.Data;

namespace ReplayParser.ReplaySorter.UI.Converters
{
    public class PathToFilenameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var filePath = value as string;
            if (filePath == null || string.IsNullOrWhiteSpace(filePath))
                return string.Empty;

            return Path.GetFileName(filePath);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
