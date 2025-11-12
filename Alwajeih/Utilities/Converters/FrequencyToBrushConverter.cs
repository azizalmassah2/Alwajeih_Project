using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using Alwajeih.Models;

namespace Alwajeih.Utilities.Converters
{
    /// <summary>
    /// Converter لتحويل نوع التحصيل إلى لون
    /// </summary>
    public class FrequencyToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is CollectionFrequency frequency)
            {
                return frequency switch
                {
                    CollectionFrequency.Daily => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3B82F6")), // أزرق - يومي
                    CollectionFrequency.Weekly => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F59E0B")), // برتقالي - أسبوعي
                    _ => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6B7280")) // رمادي - افتراضي
                };
            }

            return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6B7280"));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
