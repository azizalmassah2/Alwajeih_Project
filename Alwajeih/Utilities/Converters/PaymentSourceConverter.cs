using System;
using System.Globalization;
using System.Windows.Data;
using Alwajeih.Models;

namespace Alwajeih.Utilities.Converters
{
    /// <summary>
    /// محول مصدر الدفع من Enum إلى نص بالعربي
    /// </summary>
    public class PaymentSourceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is PaymentSource source)
            {
                return source switch
                {
                    PaymentSource.Cash => "نقدي",
                    PaymentSource.Karimi => "كريمي",
                    PaymentSource.BankTransfer => "تحويل بنكي",
                    PaymentSource.Other => "آخر",
                    _ => value.ToString()
                };
            }
            return value?.ToString() ?? string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string text)
            {
                return text switch
                {
                    "نقدي" => PaymentSource.Cash,
                    "كريمي" => PaymentSource.Karimi,
                    "تحويل بنكي" => PaymentSource.BankTransfer,
                    "آخر" => PaymentSource.Other,
                    _ => PaymentSource.Cash
                };
            }
            return PaymentSource.Cash;
        }
    }
}
