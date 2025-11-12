using System;
using System.Globalization;
using System.Windows.Data;

namespace Alwajeih.Utilities.Converters
{
    /// <summary>
    /// محول المبلغ إلى نص منسق
    /// </summary>
    public class AmountToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is decimal amount)
            {
                return $"{amount:N0} ريال";
            }
            return "0 ريال";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string str)
            {
                str = str.Replace("ريال", "").Replace(",", "").Trim();
                if (decimal.TryParse(str, out decimal result))
                {
                    return result;
                }
            }
            return 0m;
        }
    }
}
