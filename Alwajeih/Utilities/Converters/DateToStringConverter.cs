using System;
using System.Globalization;
using System.Windows.Data;
using Alwajeih.Utilities.Helpers;

namespace Alwajeih.Utilities.Converters
{
    /// <summary>
    /// محول التاريخ إلى نص منسق بالعربية
    /// </summary>
    public class DateToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DateTime date)
            {
                return DateHelper.FormatDateArabic(date);
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string str && DateTime.TryParse(str, out DateTime result))
            {
                return result;
            }
            return DateTime.Now;
        }
    }

    /// <summary>
    /// محول التاريخ إلى نص قصير
    /// </summary>
    public class DateToShortStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DateTime date)
            {
                return date.ToString("yyyy-MM-dd");
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string str && DateTime.TryParse(str, out DateTime result))
            {
                return result;
            }
            return DateTime.Now;
        }
    }
}
