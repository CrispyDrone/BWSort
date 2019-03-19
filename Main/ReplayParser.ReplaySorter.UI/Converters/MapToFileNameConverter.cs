using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace ReplayParser.ReplaySorter.UI.Converters
{
    // alternatively complete the parsing of the map, and somehow render a preview from it...
    public class MapToFileNameConverter : IValueConverter
    {
        // structure to map strings that match regex to a constant string, which is the map name that will be mapped to an image

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var mapName = value as string;
            if (string.IsNullOrWhiteSpace(mapName))
                return string.Empty;

            return $"/images/maps/fighting_spirit.jpg";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
