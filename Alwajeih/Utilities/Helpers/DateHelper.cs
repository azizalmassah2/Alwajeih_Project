using System;

namespace Alwajeih.Utilities.Helpers
{
    /// <summary>
    /// مساعد عمليات التواريخ
    /// </summary>
    public static class DateHelper
    {
        /// <summary>
        /// حساب تاريخ نهاية الحصة (182 يوم من تاريخ البداية)
        /// </summary>
        public static DateTime GetEndDate(DateTime startDate)
        {
            return startDate.AddDays(182);
        }

        /// <summary>
        /// حساب عدد الأيام المنقضية
        /// </summary>
        public static int GetDaysPassed(DateTime startDate, DateTime currentDate)
        {
            var days = (currentDate.Date - startDate.Date).Days;
            return days < 0 ? 0 : days;
        }

        /// <summary>
        /// حساب عدد الأيام المتبقية
        /// </summary>
        public static int GetDaysRemaining(DateTime endDate, DateTime currentDate)
        {
            var days = (endDate.Date - currentDate.Date).Days;
            return days < 0 ? 0 : days;
        }

        /// <summary>
        /// الحصول على تاريخ الجمعة القادمة
        /// </summary>
        public static DateTime GetNextFriday(DateTime date)
        {
            int daysUntilFriday = ((int)DayOfWeek.Friday - (int)date.DayOfWeek + 7) % 7;
            if (daysUntilFriday == 0)
                daysUntilFriday = 7;
            return date.AddDays(daysUntilFriday);
        }

        /// <summary>
        /// الحصول على تاريخ السبت الماضي (بداية الأسبوع)
        /// </summary>
        public static DateTime GetLastSaturday(DateTime date)
        {
            int daysSinceSaturday = ((int)date.DayOfWeek - (int)DayOfWeek.Saturday + 7) % 7;
            return date.AddDays(-daysSinceSaturday);
        }

        /// <summary>
        /// حساب نسبة التقدم (من 0 إلى 100)
        /// </summary>
        public static double CalculateProgress(DateTime startDate, DateTime currentDate, int totalDays = 182)
        {
            int daysPassed = GetDaysPassed(startDate, currentDate);
            if (daysPassed <= 0) return 0;
            if (daysPassed >= totalDays) return 100;
            return (daysPassed / (double)totalDays) * 100;
        }

        /// <summary>
        /// تنسيق التاريخ للعرض بالعربية
        /// </summary>
        public static string FormatDateArabic(DateTime date)
        {
            var months = new string[]
            {
                "يناير", "فبراير", "مارس", "أبريل", "مايو", "يونيو",
                "يوليو", "أغسطس", "سبتمبر", "أكتوبر", "نوفمبر", "ديسمبر"
            };

            return $"{date.Day} {months[date.Month - 1]} {date.Year}";
        }
    }
}
