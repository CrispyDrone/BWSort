using System;
using System.Globalization;
using System.Windows.Data;

namespace ReplayParser.ReplaySorter.UI.Converters
{
    public class FramesToTimeSpanInSecondsConverter : IValueConverter
    {
        private static double FastestFPS = (double)1000 / 42;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                double frames = System.Convert.ToDouble(value);
                return TimeSpan.FromSeconds(frames / FastestFPS);
            }
            catch (Exception)
            {
                return TimeSpan.Zero;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
