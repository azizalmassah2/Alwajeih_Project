using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Alwajeih.Utilities.Converters
{
    /// <summary>
    /// محول لإظهار/إخفاء عنصر بناءً على النص الفارغ
    /// يظهر العنصر عندما يكون النص فارغاً
    /// </summary>
    public class EmptyStringToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string text)
            {
                return string.IsNullOrWhiteSpace(text) ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
