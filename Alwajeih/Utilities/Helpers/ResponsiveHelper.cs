using System;
using System.Windows;

namespace Alwajeih.Utilities.Helpers
{
    /// <summary>
    /// مساعد لإدارة التجاوب بناءً على حجم الشاشة
    /// </summary>
    public static class ResponsiveHelper
    {
        /// <summary>
        /// أحجام الشاشات
        /// </summary>
        public enum ScreenSize
        {
            /// <summary>
            /// شاشة صغيرة جداً (أقل من 1024)
            /// </summary>
            ExtraSmall,
            
            /// <summary>
            /// شاشة صغيرة (1024-1366)
            /// </summary>
            Small,
            
            /// <summary>
            /// شاشة متوسطة (1367-1600)
            /// </summary>
            Medium,
            
            /// <summary>
            /// شاشة كبيرة (1601-1920)
            /// </summary>
            Large,
            
            /// <summary>
            /// شاشة كبيرة جداً (أكبر من 1920)
            /// </summary>
            ExtraLarge
        }

        /// <summary>
        /// الحصول على حجم الشاشة الحالي
        /// </summary>
        public static ScreenSize GetScreenSize()
        {
            var width = SystemParameters.PrimaryScreenWidth;

            if (width < 1024)
                return ScreenSize.ExtraSmall;
            else if (width < 1367)
                return ScreenSize.Small;
            else if (width < 1601)
                return ScreenSize.Medium;
            else if (width < 1921)
                return ScreenSize.Large;
            else
                return ScreenSize.ExtraLarge;
        }

        /// <summary>
        /// الحصول على عرض القائمة الجانبية بناءً على حجم الشاشة
        /// </summary>
        public static double GetSidebarWidth()
        {
            return GetScreenSize() switch
            {
                ScreenSize.ExtraSmall => 200,
                ScreenSize.Small => 220,
                ScreenSize.Medium => 250,
                ScreenSize.Large => 260,
                ScreenSize.ExtraLarge => 280,
                _ => 250
            };
        }

        /// <summary>
        /// الحصول على حجم الخط بناءً على حجم الشاشة
        /// </summary>
        public static double GetFontSize(FontSizeType type)
        {
            var screenSize = GetScreenSize();
            var multiplier = screenSize switch
            {
                ScreenSize.ExtraSmall => 0.85,
                ScreenSize.Small => 0.9,
                ScreenSize.Medium => 1.0,
                ScreenSize.Large => 1.05,
                ScreenSize.ExtraLarge => 1.1,
                _ => 1.0
            };

            return type switch
            {
                FontSizeType.Tiny => 10 * multiplier,
                FontSizeType.Small => 12 * multiplier,
                FontSizeType.Medium => 14 * multiplier,
                FontSizeType.Large => 16 * multiplier,
                FontSizeType.XLarge => 20 * multiplier,
                FontSizeType.XXLarge => 24 * multiplier,
                FontSizeType.Huge => 32 * multiplier,
                _ => 14 * multiplier
            };
        }

        /// <summary>
        /// الحصول على المسافة بناءً على حجم الشاشة
        /// </summary>
        public static Thickness GetMargin(MarginSize size)
        {
            var screenSize = GetScreenSize();
            var multiplier = screenSize switch
            {
                ScreenSize.ExtraSmall => 0.75,
                ScreenSize.Small => 0.85,
                ScreenSize.Medium => 1.0,
                ScreenSize.Large => 1.1,
                ScreenSize.ExtraLarge => 1.2,
                _ => 1.0
            };

            var baseValue = size switch
            {
                MarginSize.Tiny => 4,
                MarginSize.Small => 8,
                MarginSize.Medium => 12,
                MarginSize.Large => 16,
                MarginSize.XLarge => 20,
                MarginSize.XXLarge => 24,
                MarginSize.Huge => 32,
                _ => 12
            };

            var value = baseValue * multiplier;
            return new Thickness(value);
        }

        /// <summary>
        /// الحصول على عدد الأعمدة المناسب للـ Grid بناءً على حجم الشاشة
        /// </summary>
        public static int GetGridColumns()
        {
            return GetScreenSize() switch
            {
                ScreenSize.ExtraSmall => 1,
                ScreenSize.Small => 2,
                ScreenSize.Medium => 3,
                ScreenSize.Large => 4,
                ScreenSize.ExtraLarge => 4,
                _ => 3
            };
        }

        /// <summary>
        /// التحقق من أن الشاشة صغيرة
        /// </summary>
        public static bool IsSmallScreen()
        {
            var size = GetScreenSize();
            return size == ScreenSize.ExtraSmall || size == ScreenSize.Small;
        }

        /// <summary>
        /// التحقق من أن الشاشة كبيرة
        /// </summary>
        public static bool IsLargeScreen()
        {
            var size = GetScreenSize();
            return size == ScreenSize.Large || size == ScreenSize.ExtraLarge;
        }

        public enum FontSizeType
        {
            Tiny,
            Small,
            Medium,
            Large,
            XLarge,
            XXLarge,
            Huge
        }

        public enum MarginSize
        {
            Tiny,
            Small,
            Medium,
            Large,
            XLarge,
            XXLarge,
            Huge
        }
    }
}
