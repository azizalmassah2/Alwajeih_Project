using System;
using System.Globalization;
using System.Windows.Data;
using Alwajeih.Models;

namespace Alwajeih.Utilities.Converters
{
    /// <summary>
    /// محول لنوع العضو من Enum إلى نص وبالعكس
    /// </summary>
    public class MemberTypeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is MemberType memberType)
            {
                return memberType == MemberType.Regular ? "عضو أساسي" : "خلف الجمعية";
            }
            return "عضو أساسي";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string text)
            {
                return text == "خلف الجمعية" ? MemberType.BehindAssociation : MemberType.Regular;
            }
            return MemberType.Regular;
        }
    }
}
