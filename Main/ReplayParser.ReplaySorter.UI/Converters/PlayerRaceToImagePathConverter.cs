using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace ReplayParser.ReplaySorter.UI.Converters
{
    public class PlayerRaceToImagePathConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var race = value.ToString();
            if (string.IsNullOrWhiteSpace(race))
                return string.Empty;

            return $"/images/races/{race}.png";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
