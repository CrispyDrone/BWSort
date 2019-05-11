using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using ReplayParser.ReplaySorter.UI.Models;

namespace ReplayParser.ReplaySorter.UI.Converters
{
    public class FeedbackToColorConverter : IValueConverter
    {
        public static BrushConverter _bConverter = new BrushConverter();
        public static Brush _green = (Brush)_bConverter.ConvertFrom("green");
        public static Brush _red = (Brush)_bConverter.ConvertFrom("red");
        public static Brush _yellow = (Brush)_bConverter.ConvertFrom("yellow");

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var feedback = (FeedBack)value;
            switch (feedback)
            {
                case FeedBack.OK:
                    return _green;
                case FeedBack.FAILED:
                    return _red;
                case FeedBack.NONE:
                default:
                    return _yellow;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
