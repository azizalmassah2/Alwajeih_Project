using System;
using System.Globalization;
using System.Windows.Data;
using Alwajeih.Utilities.Helpers;

namespace Alwajeih.Utilities.Converters
{
    /// <summary>
    /// محول رقم اليوم إلى اسم اليوم بالعربية
    /// </summary>
    public class DayNumberToNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int dayNumber)
            {
                return WeekHelper.GetArabicDayName(dayNumber);
            }
            return value?.ToString() ?? string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
