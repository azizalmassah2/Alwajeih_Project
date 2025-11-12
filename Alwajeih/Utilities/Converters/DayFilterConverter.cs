using System;
using System.Globalization;
using System.Windows.Data;

namespace Alwajeih.Converters
{
    /// <summary>
    /// محول لعرض فلتر الأيام بشكل واضح
    /// </summary>
    public class DayFilterConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ValueTuple<int, string> tuple)
            {
                return tuple.Item2; // اسم اليوم
            }
            
            return value?.ToString() ?? "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
