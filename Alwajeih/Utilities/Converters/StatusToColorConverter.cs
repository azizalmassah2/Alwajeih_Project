using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using Alwajeih.Models;

namespace Alwajeih.Utilities.Converters
{
    /// <summary>
    /// محول حالة الحصة إلى لون
    /// </summary>
    public class PlanStatusToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is PlanStatus status)
            {
                return status switch
                {
                    PlanStatus.Active => new SolidColorBrush(Color.FromRgb(76, 175, 80)),      // أخضر
                    PlanStatus.Completed => new SolidColorBrush(Color.FromRgb(33, 150, 243)),  // أزرق
                    PlanStatus.Archived => new SolidColorBrush(Color.FromRgb(158, 158, 158)),  // رمادي
                    _ => new SolidColorBrush(Colors.Black)
                };
            }
            return new SolidColorBrush(Colors.Black);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// محول حالة الدفع إلى لون
    /// </summary>
    public class PaymentStatusToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isPaid)
            {
                return isPaid 
                    ? new SolidColorBrush(Color.FromRgb(76, 175, 80))   // أخضر - مدفوع
                    : new SolidColorBrush(Color.FromRgb(244, 67, 54));  // أحمر - غير مدفوع
            }
            return new SolidColorBrush(Colors.Black);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
