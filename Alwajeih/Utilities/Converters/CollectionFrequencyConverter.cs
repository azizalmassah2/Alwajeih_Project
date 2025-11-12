using System;
using System.Globalization;
using System.Windows.Data;
using Alwajeih.Models;

namespace Alwajeih.Utilities.Converters
{
    /// <summary>
    /// محول لنوع التحصيل من Enum إلى نص بالعربي
    /// </summary>
    public class CollectionFrequencyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is CollectionFrequency frequency)
            {
                return frequency == CollectionFrequency.Daily ? "يومي" : "أسبوعي";
            }
            
            if (value == null)
            {
                return "-";
            }
            
            return "يومي";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string text)
            {
                return text == "أسبوعي" ? CollectionFrequency.Weekly : CollectionFrequency.Daily;
            }
            return CollectionFrequency.Daily;
        }
    }
}
