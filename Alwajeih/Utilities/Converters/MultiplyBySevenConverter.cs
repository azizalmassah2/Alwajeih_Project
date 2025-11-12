using System;
using System.Globalization;
using System.Windows.Data;

namespace Alwajeih.Utilities.Converters
{
    /// <summary>
    /// Converter لضرب القيمة في 7 (لحساب المبلغ الأسبوعي)
    /// </summary>
    public class MultiplyBySevenConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is decimal decimalValue)
            {
                return decimalValue * 7;
            }
            
            if (value is double doubleValue)
            {
                return doubleValue * 7;
            }
            
            if (value is int intValue)
            {
                return intValue * 7;
            }
            
            return 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
