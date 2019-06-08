using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace ReplayParser.ReplaySorter.UI.Converters
{
    public class RenamingOutputConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var outputType = values[0] as string;
            var renamingOutputName = values[1] as string;
            switch(outputType)
            {
                case "short-path":
                    return Path.GetFileNameWithoutExtension(renamingOutputName);
                case "long-path":
                    return renamingOutputName;
                default: throw new Exception();
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
