using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using Alwajeih.Models;

namespace Alwajeih.Utilities.Converters
{
    /// <summary>
    /// محول لنوع التحصيل من Enum إلى لون
    /// </summary>
    public class CollectionFrequencyToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is CollectionFrequency frequency)
            {
                // يومي: أخضر
                if (frequency == CollectionFrequency.Daily)
                {
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981"));
                }
                // أسبوعي: أزرق
                else
                {
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3B82F6"));
                }
            }
            
            // افتراضي: رمادي
            return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8"));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
