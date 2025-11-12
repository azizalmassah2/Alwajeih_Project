using System;
using System.Collections.Generic;

namespace Alwajeih.Utilities.Helpers
{
    /// <summary>
    /// مساعد للتعامل مع نظام الأسابيع والأيام (26 أسبوع × 7 أيام = 182 يوم)
    /// </summary>
    public static class WeekHelper
    {
        public const int TotalWeeks = 26;
        public const int DaysPerWeek = 7;
        public const int TotalDays = 182; // 26 × 7

        /// <summary>
        /// أسماء الأيام بالعربي (السبت = 1)
        /// </summary>
        private static readonly Dictionary<int, string> ArabicDayNames = new()
        {
            { 1, "السبت" },
            { 2, "الأحد" },
            { 3, "الاثنين" },
            { 4, "الثلاثاء" },
            { 5, "الأربعاء" },
            { 6, "الخميس" },
            { 7, "الجمعة" },
        };

        /// <summary>
        /// الحصول على اسم اليوم بالعربي
        /// </summary>
        public static string GetArabicDayName(int dayNumber)
        {
            if (dayNumber < 1 || dayNumber > 7)
                throw new ArgumentException("رقم اليوم يجب أن يكون بين 1 و 7");

            return ArabicDayNames[dayNumber];
        }

        /// <summary>
        /// الحصول على اسم اليوم (alias لـ GetArabicDayName)
        /// </summary>
        public static string GetDayName(int dayNumber)
        {
            return GetArabicDayName(dayNumber);
        }

        /// <summary>
        /// حساب رقم الأسبوع ورقم اليوم من رقم اليوم الإجمالي (1-182)
        /// </summary>
        public static (int weekNumber, int dayNumber) GetWeekAndDay(int totalDayNumber)
        {
            if (totalDayNumber < 1 || totalDayNumber > TotalDays)
                throw new ArgumentException($"رقم اليوم يجب أن يكون بين 1 و {TotalDays}");

            int weekNumber = ((totalDayNumber - 1) / DaysPerWeek) + 1;
            int dayNumber = ((totalDayNumber - 1) % DaysPerWeek) + 1;

            return (weekNumber, dayNumber);
        }

        /// <summary>
        /// حساب رقم اليوم الإجمالي من رقم الأسبوع ورقم اليوم
        /// </summary>
        public static int GetTotalDayNumber(int weekNumber, int dayNumber)
        {
            if (weekNumber < 1 || weekNumber > TotalWeeks)
                throw new ArgumentException($"رقم الأسبوع يجب أن يكون بين 1 و {TotalWeeks}");

            if (dayNumber < 1 || dayNumber > DaysPerWeek)
                throw new ArgumentException($"رقم اليوم يجب أن يكون بين 1 و {DaysPerWeek}");

            return ((weekNumber - 1) * DaysPerWeek) + dayNumber;
        }

        /// <summary>
        /// الحصول على عرض نصي كامل لليوم والأسبوع
        /// </summary>
        public static string GetWeekDayDisplay(int weekNumber, int dayNumber)
        {
            string dayName = GetArabicDayName(dayNumber);
            return $"{dayName} - الأسبوع {weekNumber}";
        }

        /// <summary>
        /// الحصول على قائمة بجميع الأسابيع
        /// </summary>
        public static List<int> GetAllWeeks()
        {
            var weeks = new List<int>();
            for (int i = 1; i <= TotalWeeks; i++)
            {
                weeks.Add(i);
            }
            return weeks;
        }

        /// <summary>
        /// الحصول على قائمة بجميع الأيام في أسبوع معين
        /// </summary>
        public static List<(int dayNumber, string dayName)> GetDaysInWeek()
        {
            var days = new List<(int, string)>();
            for (int i = 1; i <= DaysPerWeek; i++)
            {
                days.Add((i, GetArabicDayName(i)));
            }
            return days;
        }

        /// <summary>
        /// الحصول على قائمة بجميع الأيام (alias لـ GetDaysInWeek)
        /// </summary>
        public static List<(int dayNumber, string dayName)> GetAllDays()
        {
            return GetDaysInWeek();
        }

        /// <summary>
        /// التحقق من صحة رقم الأسبوع
        /// </summary>
        public static bool IsValidWeek(int weekNumber)
        {
            return weekNumber >= 1 && weekNumber <= TotalWeeks;
        }

        /// <summary>
        /// التحقق من صحة رقم اليوم
        /// </summary>
        public static bool IsValidDay(int dayNumber)
        {
            return dayNumber >= 1 && dayNumber <= DaysPerWeek;
        }

        /// <summary>
        /// الحصول على نص وصفي للأسبوع
        /// </summary>
        public static string GetWeekDisplay(int weekNumber)
        {
            return $"الأسبوع {weekNumber}";
        }

        /// <summary>
        /// تاريخ بداية الجمعية (يتم تحميله من قاعدة البيانات)
        /// </summary>
        public static DateTime? StartDate { get; set; }

        /// <summary>
        /// حساب رقم الأسبوع واليوم من تاريخ معين
        /// </summary>
        public static (int weekNumber, int dayNumber) GetWeekAndDayFromDate(DateTime date)
        {
            // استخدام تاريخ البداية من الإعدادات أو أول سبت في السنة
            var startDate = StartDate ?? GetFirstSaturday(date.Year);
            
            // إذا كان التاريخ قبل تاريخ البداية، نستخدم سبت السنة السابقة
            if (date < startDate && !StartDate.HasValue)
            {
                startDate = GetFirstSaturday(date.Year - 1);
            }
            
            // حساب عدد الأيام من تاريخ البداية
            var daysSinceStart = (date - startDate).Days;
            
            // التأكد من أن التاريخ ليس قبل تاريخ البداية
            if (daysSinceStart < 0)
            {
                return (1, 1); // الأسبوع الأول، اليوم الأول
            }
            
            // حساب رقم اليوم الإجمالي (1-182) بدون modulo
            // إذا تجاوز 182 يوم، نستمر في العد (الأسبوع 27، 28، إلخ)
            var totalDayNumber = daysSinceStart + 1;
            
            // حساب الأسبوع واليوم
            int weekNumber = ((totalDayNumber - 1) / DaysPerWeek) + 1;
            int dayNumber = ((totalDayNumber - 1) % DaysPerWeek) + 1;
            
            return (weekNumber, dayNumber);
        }

        /// <summary>
        /// الحصول على أول سبت في السنة
        /// </summary>
        private static DateTime GetFirstSaturday(int year)
        {
            var firstDay = new DateTime(year, 1, 1);

            // حساب عدد الأيام حتى السبت القادم
            var daysUntilSaturday = ((int)DayOfWeek.Saturday - (int)firstDay.DayOfWeek + 7) % 7;

            return firstDay.AddDays(daysUntilSaturday);
        }

        /// <summary>
        /// الحصول على نطاق التواريخ لأسبوع معين (تاريخ البداية والنهاية)
        /// </summary>
        public static (DateTime startDate, DateTime endDate) GetWeekDateRange(int weekNumber)
        {
            if (!IsValidWeek(weekNumber))
                throw new ArgumentException($"رقم الأسبوع يجب أن يكون بين 1 و {TotalWeeks}");

            // استخدام تاريخ البداية من الإعدادات أو أول سبت في السنة الحالية
            var baseStartDate = StartDate ?? GetFirstSaturday(DateTime.Now.Year);

            // حساب تاريخ بداية الأسبوع المطلوب
            var weekStartDate = baseStartDate.AddDays((weekNumber - 1) * DaysPerWeek);
            
            // تاريخ نهاية الأسبوع (الجمعة)
            var weekEndDate = weekStartDate.AddDays(DaysPerWeek - 1);

            return (weekStartDate, weekEndDate);
        }

        /// <summary>
        /// الحصول على رقم الأسبوع الحالي بناءً على التاريخ الحالي
        /// </summary>
        public static int GetCurrentWeekNumber()
        {
            var today = DateTime.Now;
            var (weekNumber, _) = GetWeekAndDayFromDate(today);
            return weekNumber;
        }

        /// <summary>
        /// الحصول على رقم اليوم الحالي بناءً على التاريخ الحالي
        /// </summary>
        public static int GetCurrentDayNumber()
        {
            var today = DateTime.Now;
            var (_, dayNumber) = GetWeekAndDayFromDate(today);
            return dayNumber;
        }

        /// <summary>
        /// الحصول على الأسبوع واليوم الحاليين
        /// </summary>
        public static (int weekNumber, int dayNumber) GetCurrentWeekAndDay()
        {
            var today = DateTime.Now;
            return GetWeekAndDayFromDate(today);
        }

        /// <summary>
        /// الحصول على رقم الأسبوع من تاريخ معين
        /// </summary>
        public static int GetWeekNumber(DateTime date)
        {
            var (weekNumber, _) = GetWeekAndDayFromDate(date);
            return weekNumber;
        }

        /// <summary>
        /// الحصول على رقم اليوم من تاريخ معين
        /// </summary>
        public static int GetDayNumber(DateTime date)
        {
            var (_, dayNumber) = GetWeekAndDayFromDate(date);
            return dayNumber;
        }

        /// <summary>
        /// الحصول على التاريخ من رقم الأسبوع واليوم
        /// </summary>
        public static DateTime GetDateFromWeekAndDay(int weekNumber, int dayNumber)
        {
            if (!IsValidWeek(weekNumber) || !IsValidDay(dayNumber))
            {
                return DateTime.Now;
            }

            var (startDate, endDate) = GetWeekDateRange(weekNumber);
            return startDate.AddDays(dayNumber - 1);
        }
    }
}
