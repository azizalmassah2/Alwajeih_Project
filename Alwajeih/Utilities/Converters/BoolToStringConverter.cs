using System;
using System.Globalization;
using System.Windows.Data;

namespace Alwajeih.Utilities.Converters
{
    /// <summary>
    /// محول Boolean إلى String
    /// </summary>
    public class BoolToStringConverter : IValueConverter
    {
        public string TrueValue { get; set; } = "✏️ تعديل عضو";
        public string FalseValue { get; set; } = "➕ إضافة عضو جديد";

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? TrueValue : FalseValue;
            }
            return FalseValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
