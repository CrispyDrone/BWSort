using System.Globalization;
using System.Windows.Data;
using System;

namespace ReplayParser.ReplaySorter.UI.Converters
{
    public class DivideByParamConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!decimal.TryParse(parameter.ToString(), out var parameterAsDecimal))
                return value;

            if (!decimal.TryParse(value.ToString(), out var valueAsDecimal))
                return value;

            return decimal.Divide(valueAsDecimal, parameterAsDecimal);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
