using System;
using System.Globalization;
using System.Windows.Data;

namespace Alwajeih.Utilities.Converters
{
    /// <summary>
    /// محول Boolean معكوس (بدون Visibility)
    /// </summary>
    public class InverseBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
                return !boolValue;
            
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
                return !boolValue;
            
            return false;
        }
    }
}
